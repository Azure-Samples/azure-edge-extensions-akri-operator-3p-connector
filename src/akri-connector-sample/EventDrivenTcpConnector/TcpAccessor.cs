using System.Net.Sockets;
using System.Text.Json;
using Azure.Iot.Operations.Connector;
using Azure.Iot.Operations.Services.AssetAndDeviceRegistry.Models;
using Rfc1006LibNet.Advanced;
using Rfc1006LibNet.Advanced.EventArgs;

namespace EventDrivenTcpConnector;

public class TcpAccessor : IDisposable
{
    private readonly ConnectorWorker _connector;
    private Rfc1006Client? _rfcClient;
    private readonly ILogger<EventDrivenTcpConnectorWorker> _logger;
    private AssetAvailableEventArgs _args;
    private AssetEvent _assetEvent;
    private bool _disposed = false;

    /// <summary>
    /// c'tor
    /// </summary>
    /// <param name="connector"></param>
    /// <param name="logger"></param>
    public TcpAccessor(ConnectorWorker connector, ILogger<EventDrivenTcpConnectorWorker> logger)
    {
        _connector = connector;
        _logger = logger;
    }

    /// <summary>
    /// Opens the TCP connection and registers the callback for new events
    /// </summary>
    /// <param name="args"></param>
    /// <param name="assetEvent"></param>
    /// <param name="cancellationToken"></param>
    public void OpenTcpConnection(AssetAvailableEventArgs args, AssetEvent assetEvent, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _args = args;
                _assetEvent = assetEvent;

                if (_args.Device.Endpoints == null
                    || _args.Device.Endpoints.Inbound == null)
                {
                    _logger.LogError("Missing TCP server address configuration");
                    return;
                }

                string host = _args.Device.Endpoints.Inbound["plant_simulator_endpoint"].Address.Split(":")[0];
                int.TryParse(_args.Device.Endpoints.Inbound["plant_simulator_endpoint"].Address.Split(":")[1],
                    out int port);
                dynamic? addConfig = JsonSerializer.Deserialize<dynamic>(_args.Device.Endpoints
                    .Inbound["plant_simulator_endpoint"].AdditionalConfiguration!);
                _logger.LogInformation("Attempting to open TCP client with address {0} and port {1}", host, port);
                
                _rfcClient = new Rfc1006Client(host, port, addConfig?.localTSAP, addConfig?.remoteTSAP);
                _rfcClient.Received += OnReceived;
                _rfcClient.Connect();

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to open TCP connection to asset");
            }
        }
    }

    private void OnReceived(object? sender, TransferEventArgs args)
    {
        _logger.LogInformation(
            $"Received and publishing data for event {_assetEvent.Name} in asset {_args.AssetName}");
        _connector.ForwardReceivedEventAsync(_args.DeviceName, _args.InboundEndpointName, _args.Asset, _args.AssetName,
            _assetEvent, args.Buffer, null, CancellationToken.None);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (_rfcClient != null)
            {
                _rfcClient.Received -= OnReceived;
                _rfcClient.Dispose();
            }

            _rfcClient = null;
        }

        _disposed = true;
    }
}