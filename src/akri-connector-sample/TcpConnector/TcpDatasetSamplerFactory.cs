using Azure.Iot.Operations.Connector;
using Azure.Iot.Operations.Services.Assets;
using Rfc1006LibNet.Advanced;

namespace TcpConnector
{
	public class TcpDatasetSamplerFactory : IDatasetSamplerFactory
	{
		public static readonly Func<IServiceProvider, IDatasetSamplerFactory> TcpDatasetSamplerFactoryProvider =
			service => new TcpDatasetSamplerFactory();

		/// <summary>
		/// Creates a dataset sampler for the given dataset.
		/// </summary>
		/// <param name="aep">The asset endpoint profile to connect to when sampling this dataset.</param>
		/// <param name="asset">The asset that the dataset sampler will sample from.</param>
		/// <param name="dataset">The dataset that a sampler is needed for.</param>
		/// <returns>The dataset sampler for the provided dataset.</returns>
		public IDatasetSampler CreateDatasetSampler(AssetEndpointProfile aep, Asset asset,
			Dataset dataset)
		{
			if (!asset.DisplayName!.StartsWith("Siemens PLC") && !dataset.Name.Equals("siemens_plc"))
				throw new InvalidOperationException(
					$"Unrecognized dataset with name {dataset.Name} on asset with name {asset.DisplayName}");
			
			var localTsap = aep.AdditionalConfiguration?.RootElement.GetProperty("localTSAP").GetString();
			var remoteTsap = aep.AdditionalConfiguration?.RootElement.GetProperty("localTSAP").GetString();
			var rfcClient = new Rfc1006Client(aep.TargetAddress, localTsap, remoteTsap);
			
			return new TcpDatasetSampler(rfcClient, asset);
		}
	}
}