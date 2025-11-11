using Azure.Iot.Operations.Connector;
using TcpConnector;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(MqttSessionClientFactoryProvider.MqttSessionClientFactory);
        // services.AddSingleton(TcpDatasetSamplerFactory.TcpDatasetSamplerFactoryProvider);
        services.AddSingleton<IDatasetSamplerFactory, TcpDatasetSamplerFactory>();
        services.AddHostedService<TcpConnectorWorker>();
    })
    .Build();

host.Run();
