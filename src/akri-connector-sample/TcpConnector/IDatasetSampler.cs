using Azure.Iot.Operations.Services.AzureDeviceRegistry;
using Rfc1006LibNet.Advanced.EventArgs;

namespace TcpConnector
{
	/// <summary>
	/// A sampler of a single dataset within an asset for a TCP server.
	/// </summary>
	public interface IDatasetSampler
	{
		/// <summary>
		/// Sample the datapoints from the asset and return the full serialized dataset.
		/// </summary>
		/// <param name="dataset">The dataset of an asset to sample.</param>
		/// <param name="receiveCallback">The callback for received data from an asset</param>
		void StartSampling(Dataset dataset, EventHandler<TransferEventArgs> receiveCallback);

		/// <summary>
		/// Stop sampling and retrieving data
		/// </summary>
		void StopSampling();
	}
}