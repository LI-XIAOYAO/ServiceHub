using CliWrap;
using CliWrap.EventStream;
using Microsoft.Extensions.Options;

namespace ServiceHub
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IOptions<List<ScriptConfig>> _options;

        public Worker(ILogger<Worker> logger, ILoggerFactory loggerFactory, IOptions<List<ScriptConfig>> options)
        {
            _logger = logger;
            this.loggerFactory = loggerFactory;
            _options = options;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            base.StopAsync(cancellationToken);

            return ExecuteCommandAsync(CommandOptions.Stop, cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            await ExecuteCommandAsync(CommandOptions.Start, stoppingToken);
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="options"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private Task ExecuteCommandAsync(CommandOptions options, CancellationToken stoppingToken)
        {
            var isStart = options is CommandOptions.Start;
            if (isStart)
            {
                _logger.LogInformation($"Config {_options.Value.Count}");
            }

            // 根据配置执行命令
            foreach (var scriptConfig in _options.Value)
            {
                var logger = loggerFactory.CreateLogger($"{nameof(ServiceHub)}.{scriptConfig.Name}");
                if (!scriptConfig.Enable)
                {
                    if (isStart)
                    {
                        logger.LogInformation($"[{scriptConfig.Name}] disable");
                    }

                    continue;
                }

                logger.LogInformation($"[{scriptConfig.Name}] {(isStart ? "running" : "stopping")}...");

                // 启动不存在命令跳过，停止不校验
                if (isStart && !scriptConfig.Start.Any())
                {
                    logger.LogInformation($"[{scriptConfig.Name}] not find command");

                    continue;
                }

                // 根据配置执行命令
                _ = Task.Factory.StartNew(async () =>
                {
                    if (!isStart || scriptConfig.StopIt)
                    {
                        await ExecuteCommandAsync(scriptConfig, logger, CommandOptions.Stop, stoppingToken);
                    }

                    if (isStart)
                    {
                        await ExecuteCommandAsync(scriptConfig, logger, CommandOptions.Start, stoppingToken);
                    }
                }, TaskCreationOptions.LongRunning);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="scriptConfig"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private static async Task ExecuteCommandAsync(ScriptConfig scriptConfig, ILogger logger, CommandOptions options, CancellationToken stoppingToken)
        {
            var isError = false;
            foreach (var config in options is CommandOptions.Start ? scriptConfig.Start : scriptConfig.Stop)
            {
                // 上一条失败后不在执行
                if (isError)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(config.Command))
                {
                    logger.LogError(1, $"[{scriptConfig.Name}-{options}] Command is null");

                    continue;
                }

                var workingDirectory = config.CommandOptions is CommandOptions.Start ? scriptConfig.StartWorkingDirectory : scriptConfig.StopWorkingDirectory;
                var cmd = Cli.Wrap(Path.Combine(workingDirectory, config.Command))
                    .WithArguments(config.Arguments)
                    .WithWorkingDirectory(workingDirectory)
                    .WithValidation(CommandResultValidation.None);

                try
                {
                    await foreach (var commandEvent in cmd.ListenAsync(stoppingToken))
                    {
                        switch (commandEvent)
                        {
                            case StartedCommandEvent started:
                                logger.LogInformation(1, $"[{scriptConfig.Name}-{options}] ProcessId: {started.ProcessId}");

                                break;

                            case StandardOutputCommandEvent stdOut:
                                logger.LogInformation(1, $"[{scriptConfig.Name}-{options}] Output: {stdOut.Text}");

                                break;

                            case StandardErrorCommandEvent stdErr:
                                logger.LogError(1, $"[{scriptConfig.Name}-{options}] Output: {stdErr.Text}");
                                isError = true;

                                break;

                            case ExitedCommandEvent exited:
                                logger.LogInformation(1, $"[{scriptConfig.Name}-{options}] ExitCode: {exited.ExitCode}");

                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(1, $"[{scriptConfig.Name}-{options}] {ex.Message}");
                    isError = true;

                    return;
                }
            }
        }
    }
}