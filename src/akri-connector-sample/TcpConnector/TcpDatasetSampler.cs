using Azure.Iot.Operations.Services.Assets;
using Rfc1006LibNet.Advanced;
using Rfc1006LibNet.Advanced.EventArgs;

namespace TcpConnector
{
    /// <inheritdoc />
    internal class TcpDatasetSampler : IEventDatasetSampler
    {
        private Rfc1006Client _rfcClient;
        private DatasetSamplerContext _samplerContext;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="rfcClient">Rfc 1006 client</param>
        /// <param name="samplerContext">Dataset sampler context</param>
        public TcpDatasetSampler(Rfc1006Client rfcClient, DatasetSamplerContext samplerContext)
        {
            _rfcClient = rfcClient;
            _samplerContext = samplerContext;
        }

        public event EventHandler<TransferEventArgs>? Received;

        public async Task<byte[]> SampleDatasetAsync(Dataset dataset, CancellationToken cancellationToken = default)
        {
            try
            {
                var samplingInterval =
                    _samplerContext.Asset.DefaultDatasetsConfiguration?.RootElement.GetProperty("samplingInterval").GetDouble();
                var timeToElapse = DateTime.UtcNow.AddMilliseconds(samplingInterval ?? 0);
                
                byte[]? payload = null;
                _rfcClient.Received += (sender, args) =>
                {
                    Received?.Invoke(_samplerContext, args);
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