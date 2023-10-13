using System.Collections.Concurrent;

namespace ServiceHub.Logger
{
    /// <summary>
    /// FileLoggerProvider
    /// </summary>
    internal class FileLoggerProvider : ILoggerProvider
    {
        private Config _config = new();
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new ConcurrentDictionary<string, FileLogger>();

        public ILogger CreateLogger(string categoryName)
        {
            if (!_loggers.TryGetValue(categoryName, out var logger))
            {
                return _loggers.GetOrAdd(categoryName, new FileLogger(categoryName, _config));
            }

            return logger;
        }

        public FileLoggerProvider AddConfig(Config? config)
        {
            if (null != config)
            {
                _config = config;
            }

            return this;
        }

        public void Dispose()
        {
            return;
        }
    }
}