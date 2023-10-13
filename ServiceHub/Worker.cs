using CliWrap;
using CliWrap.EventStream;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ServiceHub
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptions<List<ScriptConfig>> _options;
        private readonly IConfiguration _configuration;
        private static readonly Process _currentProcess = Process.GetCurrentProcess();
        private static readonly string _md5;
        internal const string WorkerId = "90B77522B30542D8B7D0AFAB4119F4CC";
        internal static bool IsDaemon { get; set; }

        public Worker(ILogger<Worker> logger, ILoggerFactory loggerFactory, IOptions<List<ScriptConfig>> options, IConfiguration configuration)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _options = options;
            _configuration = configuration;
        }

        static Worker()
        {
            _md5 = MD5Hash(_currentProcess.MainModule!.FileName)!;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            base.StopAsync(cancellationToken);

            KillDaemon();

            return ExecuteCommandAsync(CommandOptions.Stop, cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DaemonAsync(stoppingToken);

            if (IsDaemon)
            {
                _logger.LogInformation($"Daemon running at: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}");

                return;
            }

            _logger.LogInformation($"Worker running at: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}");

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
                var logger = _loggerFactory.CreateLogger($"{nameof(ServiceHub)}.{scriptConfig.Name}");
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
                    _ = DaemonAsync(scriptConfig, logger, stoppingToken);

                    await ExecuteCommandAsync(isStart, scriptConfig, logger, stoppingToken);
                }, TaskCreationOptions.LongRunning);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// ExecuteCommandAsync
        /// </summary>
        /// <param name="isStart"></param>
        /// <param name="scriptConfig"></param>
        /// <param name="logger"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private static async Task ExecuteCommandAsync(bool isStart, ScriptConfig scriptConfig, ILogger logger, CancellationToken stoppingToken)
        {
            if (!isStart || scriptConfig.StopIt)
            {
                await ExecuteCommandAsync(scriptConfig, logger, CommandOptions.Stop, stoppingToken);
            }

            if (isStart)
            {
                await ExecuteCommandAsync(scriptConfig, logger, CommandOptions.Start, stoppingToken);
            }
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

        /// <summary>
        /// 命令守护
        /// </summary>
        /// <param name="scriptConfig"></param>
        /// <param name="logger"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private async Task DaemonAsync(ScriptConfig scriptConfig, ILogger logger, CancellationToken stoppingToken)
        {
            if (scriptConfig.IsCheck && !string.IsNullOrWhiteSpace(scriptConfig.CheckWords) && scriptConfig.Check.Any())
            {
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(60 * 1000);

                    var daemonLogger = _loggerFactory.CreateLogger($"{scriptConfig.Name}.Daemon");

                    if (scriptConfig.CheckDaemon < 1)
                    {
                        daemonLogger.LogInformation($"Daemon is closed.");

                        return;
                    }

                    daemonLogger.LogInformation($"Start daemon {scriptConfig.CheckDaemon}s");

                    var delay = scriptConfig.CheckDaemon * 1000;
                    while (true)
                    {
                        try
                        {
                            var isHealthy = false;
                            foreach (var command in scriptConfig.Check)
                            {
                                Command cmd;
                                if (string.IsNullOrWhiteSpace(command.Command))
                                {
                                    if (Environment.OSVersion.Platform is PlatformID.Win32NT)
                                    {
                                        cmd = Cli.Wrap("CMD").WithArguments(command.Arguments);
                                    }
                                    else
                                    {
                                        daemonLogger.LogInformation($"Platform {Environment.OSVersion.Platform} not support.");

                                        return;
                                    }
                                }
                                else
                                {
                                    cmd = Cli.Wrap(Path.Combine(scriptConfig.CheckWorkingDirectory, command.Command))
                                            .WithArguments(command.Arguments)
                                            .WithWorkingDirectory(scriptConfig.CheckWorkingDirectory)
                                            .WithValidation(CommandResultValidation.None);
                                }

                                await foreach (var commandEvent in cmd.ListenAsync(stoppingToken))
                                {
                                    if (commandEvent is StandardOutputCommandEvent stdOut)
                                    {
                                        daemonLogger.LogInformation($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}: {stdOut.Text}");

                                        if (stdOut.Text?.Contains(scriptConfig.CheckWords, StringComparison.OrdinalIgnoreCase) ?? false)
                                        {
                                            isHealthy = true;

                                            break;
                                        }
                                    }
                                }

                                if (isHealthy)
                                {
                                    break;
                                }
                            }

                            if (!isHealthy)
                            {
                                daemonLogger.LogInformation($"Healthy false, Restarting...");

                                Task.Factory.StartNew(async () =>
                                {
                                    await ExecuteCommandAsync(true, scriptConfig, logger, stoppingToken);
                                }, TaskCreationOptions.LongRunning).Wait(TimeSpan.FromSeconds(30));
                            }
                            else
                            {
                                daemonLogger.LogInformation($"Healthy true");
                            }
                        }
                        catch (Exception ex)
                        {
                            daemonLogger.LogError($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}: {ex.Message}");
                        }

                        await Task.Delay(delay);
                    }
                }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            }
        }

        /// <summary>
        /// 守护程序
        /// </summary>
        /// <returns></returns>
        private async Task DaemonAsync(CancellationToken stoppingToken)
        {
            var daemonVal = _configuration.GetValue<object>("Daemon", 3);
            if (!int.TryParse(daemonVal?.ToString(), out var daemon))
            {
                _logger.LogError($"Start daemon error, Daemon {daemonVal} is invalid.");

                return;
            }

            await Task.Factory.StartNew(async () =>
            {
                var logger = _loggerFactory.CreateLogger($"{nameof(ServiceHub)}.Daemon");

                if (daemon < 1)
                {
                    logger.LogInformation($"Daemon is closed.");

                    return;
                }

                logger.LogInformation($"Start daemon {daemon}s");

                var delay = daemon * 1000;
                Command cmd;
                if (Environment.OSVersion.Platform is PlatformID.Win32NT)
                {
                    cmd = Cli.Wrap("CMD").WithArguments($"/C FOR /F \"usebackq tokens=4\" %i IN (`SC QUERY {nameof(ServiceHub)} ^| FINDSTR STATE`) DO @ECHO %i");
                }
                else
                {
                    logger.LogInformation($"Platform {Environment.OSVersion.Platform} not support.");

                    return;
                }

                while (true)
                {
                    try
                    {
                        if (Environment.OSVersion.Platform is PlatformID.Win32NT)
                        {
                            // 判断服务是否在运行
                            await foreach (var commandEvent in cmd.ListenAsync(stoppingToken))
                            {
                                if (commandEvent is StandardOutputCommandEvent stdOut)
                                {
                                    logger.LogInformation($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}: {stdOut.Text}");

                                    // 启动服务
                                    if ("RUNNING" != stdOut.Text?.ToUpper())
                                    {
                                        _ = Cli.Wrap("CMD").WithArguments($"/C NET START {nameof(ServiceHub)}").ExecuteAsync();
                                    }
                                    else
                                    {
                                        // 启动守护程序
                                        if (!Process.GetProcessesByName(nameof(ServiceHub)).Any(c => c.Id != _currentProcess.Id && MD5Hash(c.MainModule?.FileName) == _md5))
                                        {
                                            _ = Cli.Wrap($"{nameof(ServiceHub)}.exe").WithArguments(WorkerId).ExecuteAsync(stoppingToken);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}: {ex.Message}");
                    }

                    await Task.Delay(delay);
                }
            }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        /// <summary>
        /// 关闭守护程序
        /// </summary>
        private void KillDaemon()
        {
            if (IsDaemon)
            {
                return;
            }

            try
            {
                foreach (var process in Process.GetProcessesByName(nameof(ServiceHub)))
                {
                    if (process.Id != _currentProcess.Id && MD5Hash(process.MainModule?.FileName) == _md5)
                    {
                        process.Kill();

                        _logger.LogInformation($"Kill daemon {process.Id}.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kill daemon（{ex.Message}）.");
            }
        }

        /// <summary>
        /// 最大进程数
        /// </summary>
        /// <param name="processCount"></param>
        internal static void KillCurrentProcess(int processCount)
        {
            if (Process.GetProcessesByName(nameof(ServiceHub)).Length > processCount)
            {
                Environment.Exit(Environment.ExitCode);
            }
        }

        /// <summary>
        /// 计算文件MD5
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string? MD5Hash(string? path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var bufferSize = 1024 * 64;
            var input = new byte[bufferSize];
            var output = new byte[bufferSize];
            int readLength = 0;
            using var md5 = MD5.Create();
            using var inputStream = File.OpenRead(path);
            while ((readLength = inputStream.Read(input, 0, input.Length)) > 0)
            {
                md5.TransformBlock(input, 0, readLength, output, 0);
            }

            md5.TransformFinalBlock(input, 0, 0);

            return BitConverter.ToString(md5.Hash!).Replace("-", null);
        }
    }
}