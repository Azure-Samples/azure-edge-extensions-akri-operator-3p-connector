using Azure.Iot.Operations.Services.Assets;
using Rfc1006LibNet.Advanced;
using Rfc1006LibNet.Advanced.EventArgs;

namespace TcpConnector
{
	/// <inheritdoc />
	public class TcpDatasetSampler : IEventDatasetSampler
	{
		private Rfc1006Client _rfcClient;
		private DatasetSamplerContext _samplerContext;
		private ILogger<TcpDatasetSampler> _logger;

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="rfcClient">Rfc 1006 client</param>
		/// <param name="samplerContext">Dataset sampler context</param>
		/// <param name="logger">logger</param>
		public TcpDatasetSampler(Rfc1006Client rfcClient, DatasetSamplerContext samplerContext,
			ILogger<TcpDatasetSampler> logger)
		{
			_rfcClient = rfcClient;
			_samplerContext = samplerContext;
			_logger = logger;
		}

		public event EventHandler<TransferEventArgs>? Received;

		public async Task<byte[]> SampleDatasetAsync(Dataset dataset, CancellationToken cancellationToken = default)
		{
			_logger.LogInformation("Enter SampleDatasetAsync");

			try
			{
				_rfcClient.Received += OnReceived;
				_rfcClient.Connect();
				
				_logger.LogInformation($"Tcp connection for dataset {dataset.Name} established.");
				
				return null;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to initialize TCP server", ex);
			}
		}

		private void OnReceived(object? sender, TransferEventArgs args)
		{
			Received?.Invoke(_samplerContext, args);
		}
	}
}