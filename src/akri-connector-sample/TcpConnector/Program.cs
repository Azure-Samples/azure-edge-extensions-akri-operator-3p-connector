using TcpConnector;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(MqttSessionClientFactoryProvider.MqttSessionClientFactory);
        services.AddSingleton(TcpDatasetSamplerFactory.TcpDatasetSamplerFactoryProvider);
        services.AddHostedService<TcpConnectorAppWorker>();
    })
    .Build();

host.Run();
