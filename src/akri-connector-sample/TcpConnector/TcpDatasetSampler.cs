using Azure.Iot.Operations.Services.AzureDeviceRegistry;
using Rfc1006LibNet.Advanced;
using Rfc1006LibNet.Advanced.EventArgs;

namespace TcpConnector
{
    /// <inheritdoc />
    internal class TcpDatasetSampler : IDatasetSampler
    {
        private Rfc1006Client _rfcClient;
        private string _assetName;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="rfcClient"></param>
        /// <param name="assetName"></param>
        public TcpDatasetSampler(Rfc1006Client rfcClient, string assetName)
        {
            _rfcClient = rfcClient;
            _assetName = assetName;
        }

        public void Initialize(EventHandler<TransferEventArgs> receiveCallback)
        {
        }

        public void StartSampling(Dataset dataset, EventHandler<TransferEventArgs> receiveCallback)
        {
            try
            {
                _rfcClient.Received += receiveCallback;
                _rfcClient.Connect();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize TCP server", ex);
            }
        }

        public void StopSampling()
        {
            _rfcClient.Stop();
        }
    }
}