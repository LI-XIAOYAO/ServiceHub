using ServiceHub;
using ServiceHub.Logger;

Worker.KillCurrentProcess(2);

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSystemd()
    .ConfigureServices(services =>
    {
        Worker.IsDaemon = 1 == args.Length && Worker.WorkerId == args[0];
        services.AddHostedService<Worker>();
        services.Configure<List<ScriptConfig>>(services.BuildServiceProvider().GetRequiredService<IConfiguration>().GetSection("Config"));
    })
    .ConfigureLogging((context, logging) =>
    {
        var config = context.Configuration.GetSection("Logging:File").Get<Config>();
        if (config?.Enable ?? false)
        {
            logging.AddProvider(new FileLoggerProvider().AddConfig(config));
        }
    })
    .Build();

host.Run();