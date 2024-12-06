using Azure.Iot.Operations.Connector;
using Azure.Iot.Operations.Services.Assets;
using Rfc1006LibNet.Advanced;

namespace TcpConnector
{
	public class TcpDatasetSamplerFactory : IDatasetSamplerFactory
	{
		private readonly ILogger<TcpDatasetSampler> _logger;

		// public static readonly Func<IServiceProvider, IDatasetSamplerFactory> TcpDatasetSamplerFactoryProvider =
		// 	service => new TcpDatasetSamplerFactory();

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="logger">logger</param>
		public TcpDatasetSamplerFactory(ILogger<TcpDatasetSampler> logger)
		{
			_logger = logger;
		}

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
			_logger.LogInformation("Enter CreateDatasetSampler");

			if (!asset.DisplayName!.StartsWith("Siemens PLC") && !dataset.Name.Equals("siemens_plc"))
				throw new InvalidOperationException(
					$"Unrecognized dataset with name {dataset.Name} on asset with name {asset.DisplayName}");
			
			var url = new Uri(aep.TargetAddress);
			var localTsap = aep.AdditionalConfiguration?.RootElement.GetProperty("localTSAP").GetString();
			var remoteTsap = aep.AdditionalConfiguration?.RootElement.GetProperty("localTSAP").GetString();
			var rfcClient = new Rfc1006Client(url.Host, url.Port, localTsap, remoteTsap);
			
			return new TcpDatasetSampler(rfcClient, new DatasetSamplerContext(aep, asset, dataset.Name), _logger);
		}
	}
}