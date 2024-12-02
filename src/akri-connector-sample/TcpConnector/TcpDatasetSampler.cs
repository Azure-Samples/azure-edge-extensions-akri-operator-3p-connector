using Azure.Iot.Operations.Services.Assets;
using Azure.Iot.Operations.Connector;
using Rfc1006LibNet.Advanced;
using Rfc1006LibNet.Advanced.EventArgs;

namespace TcpConnector
{
    /// <inheritdoc />
    internal class TcpDatasetSampler : IDatasetSampler
    {
        private Rfc1006Client _rfcClient;
        private Asset _asset;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="rfcClient"></param>
        /// <param name="asset"></param>
        public TcpDatasetSampler(Rfc1006Client rfcClient, Asset asset)
        {
            _rfcClient = rfcClient;
            _asset = asset;
        }

        public async Task<byte[]> SampleDatasetAsync(Dataset dataset, CancellationToken cancellationToken = default)
        {
            try
            {
                var samplingInterval =
                    _asset.DefaultDatasetsConfiguration?.RootElement.GetProperty("samplingInterval").GetDouble();
                var timeToElapse = DateTime.UtcNow.AddMilliseconds(samplingInterval ?? 0);
                
                byte[]? payload = null;
                _rfcClient.Received += (sender, args) =>
                {
                    payload = args.Buffer;
                } ;
                _rfcClient.Connect();

                while (DateTime.UtcNow > timeToElapse)
                {
                    if (null != payload)
                        break;
                }
                
                _rfcClient.Stop();
                return payload!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize TCP server", ex);
            }
        }
    }
}