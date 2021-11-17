using wprops_demo;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<DeviceRunner>();
    })
    .Build();

await host.RunAsync();
