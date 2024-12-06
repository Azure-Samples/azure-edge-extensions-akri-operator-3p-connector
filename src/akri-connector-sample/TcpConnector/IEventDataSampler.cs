using Azure.Iot.Operations.Connector;
using Rfc1006LibNet.Advanced.EventArgs;

namespace TcpConnector;

public interface IEventDatasetSampler : IDatasetSampler
{
	event EventHandler<TransferEventArgs> Received;
}