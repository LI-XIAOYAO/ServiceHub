using ServiceHub;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSystemd()
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.Configure<List<ScriptConfig>>(services.BuildServiceProvider().GetRequiredService<IConfiguration>().GetSection("Config"));
    })
    .ConfigureLogging(logging => logging.AddEventLog(configure => configure.LogName = configure.SourceName = nameof(ServiceHub)))
    .Build();

host.Run();