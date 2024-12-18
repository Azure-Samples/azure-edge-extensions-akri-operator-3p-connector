using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Azure.Iot.Operations.Connector;
using Azure.Iot.Operations.Mqtt.Session;
using Azure.Iot.Operations.Protocol.Connection;
using Azure.Iot.Operations.Protocol.Models;
using Azure.Iot.Operations.Services.Assets;
using Azure.Iot.Operations.Services.LeaderElection;
using Rfc1006LibNet.Advanced.EventArgs;

namespace TcpConnector
{
    public class TcpConnectorWorker : ConnectorWorker
    {
        private readonly ILogger<TcpConnectorWorker> _logger;
        private readonly MqttSessionClient _sessionClient;
        private readonly IDatasetSamplerFactory _datasetSamplerFactory;

        // Mapping of asset name to the dictionary that maps a dataset name to its sampler
        private readonly Dictionary<string, Dictionary<string, IEventDatasetSampler>> _datasetSamplers = new();

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="logger">logger</param>
        /// <param name="mqttSessionClient">mqttSessionClient</param>
        /// <param name="datasetSamplerFactory">datasetSamplerFactory</param>
        public TcpConnectorWorker(
            ILogger<TcpConnectorWorker> logger, 
            MqttSessionClient mqttSessionClient, 
            IDatasetSamplerFactory datasetSamplerFactory) : base(logger, mqttSessionClient, datasetSamplerFactory)
        {
            _logger = logger;
            _sessionClient = mqttSessionClient;
            _datasetSamplerFactory = datasetSamplerFactory;
        }
        
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //TODO do active passive LE in the template level. Check replica count > 1 in connector config works as expected
            string candidateName = Guid.NewGuid().ToString();
            bool isLeader = false;

            // Create MQTT client from credentials provided by the operator
            MqttConnectionSettings mqttConnectionSettings = MqttConnectionSettings.FromFileMount();
            mqttConnectionSettings.ClientId = Guid.NewGuid().ToString(); //TODO get from config
            _logger.LogInformation($"Connecting to MQTT broker with {mqttConnectionSettings}");

            //TODO retry if it fails, but wait until what to try again? Just rely on retry policy?
            await _sessionClient.ConnectAsync(mqttConnectionSettings, cancellationToken);

            _logger.LogInformation($"Successfully connected to MQTT broker");

            bool doingLeaderElection = false;
            TimeSpan leaderElectionTermLength = TimeSpan.FromSeconds(5);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        AssetMonitor assetMonitor = new AssetMonitor();

                        TaskCompletionSource aepDeletedOrUpdatedTcs = new();
                        TaskCompletionSource<AssetEndpointProfile> aepCreatedTcs = new();
                        assetMonitor.AssetEndpointProfileChanged += (sender, args) =>
                        {
                            // Each connector should have one AEP deployed to the pod. It shouldn't ever be deleted, but it may be updated.
                            if (args.ChangeType == ChangeType.Created)
                            {
                                if (args.AssetEndpointProfile == null)
                                {
                                    // shouldn't ever happen
                                    _logger.LogError("Received notification that asset endpoint profile was created, but no asset endpoint profile was provided");
                                }
                                else
                                {
                                    aepCreatedTcs.TrySetResult(args.AssetEndpointProfile);
                                }
                            }
                            else
                            {
                                aepDeletedOrUpdatedTcs.TrySetResult();
                            }
                        };

                        assetMonitor.ObserveAssetEndpointProfile(null, cancellationToken);

                        _logger.LogInformation("Waiting for asset endpoint profile to be discovered");
                        AssetEndpointProfile assetEndpointProfile = await aepCreatedTcs.Task.WaitAsync(cancellationToken);

                        _logger.LogInformation("Successfully discovered the asset endpoint profile");

                        if (assetEndpointProfile.AdditionalConfiguration != null
                            && assetEndpointProfile.AdditionalConfiguration.RootElement.TryGetProperty("leadershipPositionId", out JsonElement value)
                            && value.ValueKind == JsonValueKind.String
                            && value.GetString() != null)
                        {
                            doingLeaderElection = true;
                            string leadershipPositionId = value.GetString()!;

                            _logger.LogInformation($"Leadership position Id {leadershipPositionId} was configured, so this pod will perform leader election");

                            await using LeaderElectionClient leaderElectionClient = new(_sessionClient, leadershipPositionId, candidateName);

                            leaderElectionClient.AutomaticRenewalOptions = new LeaderElectionAutomaticRenewalOptions()
                            {
                                AutomaticRenewal = true,
                                ElectionTerm = leaderElectionTermLength,
                                RenewalPeriod = leaderElectionTermLength.Subtract(TimeSpan.FromSeconds(1))
                            };

                            leaderElectionClient.LeadershipChangeEventReceivedAsync += (sender, args) =>
                            {
                                isLeader = args.NewLeader != null && args.NewLeader.GetString().Equals(candidateName);
                                if (isLeader)
                                {
                                    _logger.LogInformation("Received notification that this pod is the leader");
                                }

                                return Task.CompletedTask;
                            };

                            //TODO how does this work when the DSS store shouldn't be touched? There is no way to know for sure if you are still leader without
                            //polling. Maybe it is fine if there is some overlap with 2 pods active for (campaign-length) amount of time?
                            _logger.LogInformation("This pod is waiting to be elected leader.");
                            await leaderElectionClient.CampaignAsync(leaderElectionTermLength);
                            
                            _logger.LogInformation("This pod was elected leader.");
                        }

                        assetMonitor.AssetChanged += (sender, args) =>
                        {
                            _logger.LogInformation($"Received a notification an asset with name {args.AssetName} has been {args.ChangeType.ToString().ToLower()}.");

                            if (args.ChangeType == ChangeType.Deleted)
                            {
                                StopSamplingAsset(args.AssetName);
                            }
                            else if (args.ChangeType == ChangeType.Created)
                            {
                                StartSamplingAsset(assetEndpointProfile, args.Asset!, cancellationToken);
                            }
                            else
                            {
                                // asset changes don't all necessitate re-creating the relevant dataset samplers, but there is no way to know
                                // at this level what changes are dataset-specific nor which of those changes require a new sampler. Because
                                // of that, this sample just assumes all asset changes require the factory requesting a new sampler.
                                StopSamplingAsset(args.AssetName);
                                StartSamplingAsset(assetEndpointProfile, args.Asset!, cancellationToken);
                            }
                        };

                        _logger.LogInformation("Now monitoring for asset creation/deletion/updates");
                        assetMonitor.ObserveAssets(null, cancellationToken);

                        // Wait until the worker is cancelled or it is no longer the leader
                        while (!cancellationToken.IsCancellationRequested && (isLeader || !doingLeaderElection) && !aepDeletedOrUpdatedTcs.Task.IsCompleted)
                        {
                            try
                            {
                                if (doingLeaderElection)
                                {
                                    await Task.WhenAny(
                                        aepDeletedOrUpdatedTcs.Task,
                                        Task.Delay(leaderElectionTermLength)).WaitAsync(cancellationToken);
                                }
                                else
                                {
                                    await Task.WhenAny(
                                        aepDeletedOrUpdatedTcs.Task).WaitAsync(cancellationToken);

                                }
                            }
                            catch (OperationCanceledException)
                            { 
                                // expected outcome, allow the while loop to check status again
                            }
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("This pod is shutting down. It will now stop monitoring and sampling assets.");
                        }
                        else if (aepDeletedOrUpdatedTcs.Task.IsCompleted)
                        {
                            _logger.LogInformation("Received a notification that the asset endpoint profile has changed. This pod will now cancel current asset sampling and restart monitoring assets.");
                        }
                        else if (doingLeaderElection)
                        {
                            _logger.LogInformation("This pod is no longer the leader. It will now stop monitoring and sampling assets.");
                        }
                        else
                        { 
                            // Shouldn't happen. The pod should either be cancelled, the AEP should have changed, or this pod should have lost its position as leader
                            _logger.LogInformation("This pod will now cancel current asset sampling and restart monitoring assets.");
                        }

                        foreach (Dictionary<string, IEventDatasetSampler> datasetSamplers in _datasetSamplers.Values)
                        {
                            foreach (IEventDatasetSampler datasetSampler in datasetSamplers.Values)
                            {
                                datasetSampler.Received -= DatasetSamplerOnReceived;
                            }
                        }

                        _datasetSamplers.Clear();
                        assetMonitor.UnobserveAssets();
                        assetMonitor.UnobserveAssetEndpointProfile();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Encountered an error: {ex}");
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Shutting down sample...");
            }
        }

        private void StopSamplingAsset(string assetName)
        {
            // Stop sampling this asset since it was deleted
            foreach (var datasetSampler in _datasetSamplers[assetName].Values)
            {
                datasetSampler.Received -= DatasetSamplerOnReceived;
            }

            _datasetSamplers.Remove(assetName);
        }

        private void StartSamplingAsset(AssetEndpointProfile assetEndpointProfile, Asset asset, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Enter StartSamplingAsset");
            string assetName = asset.DisplayName!;

            _datasetSamplers[assetName] = new();
            if (asset.DatasetsDictionary == null)
            {
                _logger.LogInformation($"Asset with name {assetName} has no datasets to sample");
                return;
            }
            else
            { 
                foreach (string datasetName in asset.DatasetsDictionary!.Keys)
                {
                    Dataset dataset = asset.DatasetsDictionary![datasetName];

                    SampleDataset(new DatasetSamplerContext(assetEndpointProfile, asset, datasetName));
                    
                    string mqttMessageSchema = dataset.GetMqttMessageSchema();
                    _logger.LogInformation($"Derived the schema for dataset with name {datasetName} in asset with name {assetName}:");
                    _logger.LogInformation(mqttMessageSchema);

                    //TODO register the message schema with the schema registry service
                }
            }
        }

        private async void SampleDataset(object? status)
        {
            _logger.LogInformation("Enter SampleDataset");

            DatasetSamplerContext samplerContext = (DatasetSamplerContext)status!;

            Asset asset = samplerContext.Asset;
            string datasetName = samplerContext.DatasetName;

            Dictionary<string, Dataset>? assetDatasets = asset.DatasetsDictionary;
            if (assetDatasets == null || !assetDatasets.ContainsKey(datasetName))
            {
                _logger.LogInformation($"Dataset with name {datasetName} in asset with name {samplerContext.Asset.DisplayName} was deleted. This sample won't sample this dataset anymore.");
                return;
            }

            Dataset dataset = assetDatasets[datasetName];

            if (!_datasetSamplers[asset.DisplayName!].ContainsKey(datasetName))
            {
                _datasetSamplers[asset.DisplayName!].TryAdd(datasetName,
                    (_datasetSamplerFactory.CreateDatasetSampler(samplerContext.AssetEndpointProfile, asset, dataset) as
                        IEventDatasetSampler)!);
                _logger.LogInformation($"DatasetSampler for dataset {datasetName} in asset {samplerContext.Asset.DisplayName} was created.");
            }

            if (!_datasetSamplers[asset.DisplayName!].TryGetValue(datasetName, out IEventDatasetSampler? datasetSampler))
            {
                _logger.LogInformation($"Dataset with name {datasetName} in asset with name {samplerContext.Asset.DisplayName} was deleted. This sample won't sample this dataset anymore.");
                return;
            }
            
            datasetSampler.Received += DatasetSamplerOnReceived;
            await datasetSampler.SampleDatasetAsync(dataset);
        }

        private async void DatasetSamplerOnReceived(object? sender, TransferEventArgs args)
        {
            try
            {

                var samplerContext = (DatasetSamplerContext)sender!;
                var payload = args.Buffer;
                _logger.LogInformation($"Read dataset with name {samplerContext.DatasetName} from asset with name {samplerContext.Asset.DisplayName}. Now publishing it to MQTT broker: {Encoding.UTF8.GetString(payload)}");
                
                Dataset dataset = samplerContext.Asset.DatasetsDictionary![samplerContext.DatasetName];
                var topic = dataset.Topic != null ? dataset.Topic! : samplerContext.Asset.DefaultTopic!;
                var mqttMessage = new MqttApplicationMessage(topic.Path!)
                {
                    PayloadSegment = payload,
                    Retain = topic.Retain == RetainHandling.Keep,
                };

                var puback = await _sessionClient.PublishAsync(mqttMessage);

                if (puback.ReasonCode == MqttClientPublishReasonCode.Success
                    || puback.ReasonCode == MqttClientPublishReasonCode.NoMatchingSubscribers)
                {
                    // NoMatchingSubscribers case is still successful in the sense that the PUBLISH packet was delivered to the broker successfully.
                    // It does suggest that the broker has no one to send that PUBLISH packet to, though.
                    _logger.LogInformation($"Message was accepted by the MQTT broker with PUBACK reason code: {puback.ReasonCode} and reason {puback.ReasonString}");
                }
                else
                {
                    _logger.LogInformation($"Received unsuccessful PUBACK from MQTT broker: {puback.ReasonCode} with reason {puback.ReasonString}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Encountered an error for callback: {ex}");
            }
        }
    }
}