// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Sockets;
using System.Text.Json;
using Azure.Iot.Operations.Connector;
using Azure.Iot.Operations.Protocol;
using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
using Rfc1006LibNet.Advanced;
using Rfc1006LibNet.Advanced.EventArgs;

namespace EventDrivenTcpConnector
{
    public class EventDrivenTcpConnectorWorker : BackgroundService, IDisposable
    {
        private readonly ILogger<EventDrivenTcpConnectorWorker> _logger;
        private readonly ConnectorWorker _connector;
        private readonly Dictionary<string, TcpAccessor> _tcpClients;

        /// <summary>
        /// c'tor
        /// </summary>
        /// <param name="applicationContext"></param>
        /// <param name="logger"></param>
        /// <param name="connectorLogger"></param>
        /// <param name="mqttClient"></param>
        /// <param name="datasetSamplerFactory"></param>
        /// <param name="adrClientFactory"></param>
        /// <param name="leaderElectionConfigurationProvider"></param>
        /// <param name="rfcClient"></param>
        public EventDrivenTcpConnectorWorker(ApplicationContext applicationContext, ILogger<EventDrivenTcpConnectorWorker> logger, ILogger<ConnectorWorker> connectorLogger, IMqttClient mqttClient, IMessageSchemaProvider datasetSamplerFactory, IAdrClientWrapperProvider adrClientFactory, IConnectorLeaderElectionConfigurationProvider leaderElectionConfigurationProvider, Rfc1006Client rfcClient)
        {
            _logger = logger;
            _tcpClients = new Dictionary<string, TcpAccessor>();
            _connector = new(applicationContext, connectorLogger, mqttClient, datasetSamplerFactory, adrClientFactory, leaderElectionConfigurationProvider)
            {
                WhileAssetIsAvailable = WhileAssetAvailableAsync
            };
        }

        /// <summary>
        /// Callback for available assets (and also newly created assets)
        /// </summary>
        /// <param name="args"></param>
        /// <param name="cancellationToken"></param>
        private async Task WhileAssetAvailableAsync(AssetAvailableEventArgs args, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Asset with name {0} is now sampleable", args.AssetName);
            cancellationToken.ThrowIfCancellationRequested();

            if (args.Asset.EventGroups == null || args.Asset.EventGroups.Count != 1)
            {
                _logger.LogError("Asset with name {0} does not have the expected event group", args.AssetName);
                return;
            }

            var eventGroup = args.Asset.EventGroups.First();
            if (eventGroup.Events == null || eventGroup.Events.Count != 1)
            {
                _logger.LogError("Asset with name {0} does not have the expected event within its event group", args.AssetName);
                return;
            }
            var tcpAccessor = new TcpAccessor(_connector, _logger);
            _tcpClients.Add(args.AssetName, tcpAccessor);
            // This sample only has one asset with one event
            var assetEvent = eventGroup.Events[0];
            
            tcpAccessor.OpenTcpConnection(args, assetEvent, cancellationToken);
        }

        /// <summary>
        /// Execute the worker
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting the connector...");
            await _connector.RunConnectorAsync(cancellationToken);
        }

        /// <summary>
        /// Dispose the resources and deregister callbacks
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            
            foreach (var tcpClient in _tcpClients.Values)
            {
                tcpClient.Dispose();
            }
            _tcpClients.Clear();
            
            _connector.WhileAssetIsAvailable -= WhileAssetAvailableAsync;
            _connector.Dispose();
        }
    }
}