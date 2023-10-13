using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace ServiceHub.Logger
{
    /// <summary>
    /// FileLogger
    /// </summary>
    internal class FileLogger : ILogger
    {
        private readonly Config _config;
        private readonly Channel<(LogLevel logLevel, EventId eventId, string? state, Exception? exception)> _queue = Channel.CreateUnbounded<(LogLevel logLevel, EventId eventId, string? state, Exception? exception)>();
        private readonly Mutex _mutex = new(false, typeof(FileLogger).FullName);

        /// object <summary>
        /// CategoryName
        /// </summary>
        public string CategoryName { get; }

        public FileLogger(string categoryName, Config config)
        {
            CategoryName = categoryName;
            _config = config;
            _ = ChannelReader();
        }

        /// <summary>
        /// ChannelReader
        /// </summary>
        /// <returns></returns>
        private async Task ChannelReader()
        {
            while (await _queue.Reader.WaitToReadAsync())
            {
                if (_queue.Reader.TryRead(out var message))
                {
                    try
                    {
                        _mutex.WaitOne();

                        await Log(message.logLevel, message.eventId, message.state, message.exception);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        _mutex.ReleaseMutex();
                    }
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _queue.Writer.TryWrite((logLevel, eventId, state?.ToString(), exception));
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private async Task Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception)
        {
            var now = DateTimeOffset.Now;
            string nowYear = now.Year.ToString();
            string nowMonth = now.ToString("MM");
            string nowDay = now.ToString("dd");
            string nowHour = now.ToString("HH");
            string nowMinute = now.ToString("mm");

            string logPath = Path.Combine(_config.AbsolutePath, nowYear, nowMonth, nowDay);
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            string logFilePath = Path.Combine(logPath, $"{nowHour}.{logLevel}.log");
            var isNewFile = !File.Exists(logFilePath);

            if (!isNewFile)
            {
                isNewFile = MoveFile(logLevel, nowHour, logPath, logFilePath);
            }

            using var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);

            if (isNewFile)
            {
                await streamWriter.WriteLineAsync($"{_config.TitleFormatter.Title}");
            }
            else if (null != _config.SplitLine)
            {
                await streamWriter.WriteLineAsync(_config.SplitLine);
            }

            await streamWriter.WriteLineAsync(_config.TitleFormatter.FormatterRegulation.Invoke(logLevel, eventId, Environment.CurrentManagedThreadId, now, CategoryName));
            await streamWriter.WriteLineAsync(state?.ToString());

            if (null != exception)
            {
                await streamWriter.WriteLineAsync($"Msg：{exception.GetType()}（{exception.Message}）");
                await streamWriter.WriteLineAsync($"StackTrace：{(null == exception.StackTrace ? null : string.Concat(exception.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Select(c => $"{Environment.NewLine}\t" + (c.StartsWith("   ") ? c.Remove(0, "   ".Length) : c))))}");
                await SetInnerException(exception, streamWriter);
            }

            RecoveryFiles();
        }

        /// <summary>
        /// MoveFile
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="nowHour"></param>
        /// <param name="logPath"></param>
        /// <param name="logFilePath"></param>
        /// <returns></returns>
        private bool MoveFile(LogLevel logLevel, string nowHour, string logPath, string logFilePath)
        {
            if (new FileInfo(logFilePath).Length > _config.Size * 1024)
            {
                var fileInfos = new DirectoryInfo(logPath).GetFiles();
                var regex = new Regex($@"{nowHour}.{logLevel}.(\d+).mlog", RegexOptions.IgnoreCase);
                var matchRess = fileInfos.Where(c => regex.IsMatch(c.Name)).Select(d => Convert.ToInt32(regex.Match(d.Name).Groups[1].Value));

                File.Move(logFilePath, Path.Combine(logPath, $"{nowHour}.{logLevel}.{(matchRess.Any() ? (matchRess.Max() + 1).ToString().PadLeft(2, '0') : "01")}.log"));

                return true;
            }

            return false;
        }

        /// <summary>
        /// SetInnerException
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="streamWriter"></param>
        public static async Task SetInnerException(Exception exception, StreamWriter streamWriter)
        {
            await SetInnerException(exception, streamWriter, 1);
        }

        /// <summary>
        /// SetInnerException
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="streamWriter"></param>
        /// <param name="index"></param>
        private static async Task SetInnerException(Exception exception, StreamWriter streamWriter, int index = 1)
        {
            if (exception.InnerException is null)
            {
                return;
            }

            await streamWriter.WriteLineAsync($"{string.Join("\t", new string[index])}InnerException：");
            await streamWriter.WriteLineAsync($"{string.Join("\t", new string[index + 1])}Msg：{exception.InnerException.Message}");
            await streamWriter.WriteLineAsync($"{string.Join("\t", new string[index + 1])}StackTrace：{(null == exception.InnerException.StackTrace ? null : string.Concat(exception.InnerException.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Select(c => Environment.NewLine + string.Join($"\t", new string[index + 2]) + (c.StartsWith("   ") ? c.Remove(0, "   ".Length) : c))))}");

            await SetInnerException(exception.InnerException, streamWriter, ++index);
        }

        /// <summary>
        /// RecoveryFiles
        /// </summary>
        private void RecoveryFiles(string? rootPath = null)
        {
            if (_config.AutoRecoveryLogFile!.Value && _config.AutoRecoveryBeforeDay!.Value > 0)
            {
                try
                {
                    var date = DateTime.Now.Date.AddDays(-_config.AutoRecoveryBeforeDay.Value);
                    var paths = Directory.GetDirectories(rootPath ?? _config.AbsolutePath);
                    foreach (var path in paths)
                    {
                        if (File.GetCreationTime(path) < date)
                        {
                            Directory.Delete(path);
                        }
                        else
                        {
                            RecoveryFiles(path);
                        }
                    }

                    var files = Directory.GetFiles(rootPath ?? _config.AbsolutePath);
                    foreach (var file in files)
                    {
                        if (File.GetCreationTime(file) < date)
                        {
                            File.Delete(file);
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}