using Azure.Iot.Operations.Services.Assets;

namespace TcpConnector
{
	/// <summary>
	/// A bundle of asset name + dataset name in one class to fit how <see cref="Timer"/> passes around context
	/// </summary>
	public class DatasetSamplerContext
	{
		internal AssetEndpointProfile AssetEndpointProfile { get; set; }

		internal Asset Asset { get; set; }

		internal string DatasetName { get; set; }

		internal DatasetSamplerContext(AssetEndpointProfile assetEndpointProfile, Asset asset, string datasetName)
		{
			AssetEndpointProfile = assetEndpointProfile;
			Asset = asset;
			DatasetName = datasetName;
		}
	}
}