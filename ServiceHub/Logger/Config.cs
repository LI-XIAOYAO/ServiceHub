namespace ServiceHub.Logger
{
    internal class Config
    {
        private int _size = 512;
        private string _formatter = "[{Category}] {DateTime:yyyy-MM-dd HH:mm:ss.fff} {LogLevel} {EventId} {ThreadId}";
        private TitleFormatter? _titleFormatter;

        public bool Enable { get; set; }

        /// <summary>
        /// Size：KB (Default：512 KB)
        /// </summary>
        public int? Size
        {
            get => _size;
            set
            {
                _size = value.HasValue && value > 0 ? value.Value : 512;
            }
        }

        /// <summary>
        /// Formatter
        /// <para>Parms: {DateTime:format|Category|LogLevel|EventId|ThreadId</para>
        /// </summary>
        public string? Formatter
        {
            get => _formatter;
            set => TitleFormatter = new TitleFormatter(_formatter = value ?? _formatter);
        }

        /// <summary>
        /// TitleFormatter
        /// </summary>
        internal TitleFormatter TitleFormatter
        {
            get
            {
                if (null == _titleFormatter)
                {
                    _titleFormatter = new TitleFormatter(_formatter);
                }

                return _titleFormatter;
            }
            private set => _titleFormatter = value;
        }

        /// <summary>
        /// SplitLine
        /// </summary>
        public string? SplitLine { get; set; } = "------------------------------------------------------------------------------------------------------------------------";

        /// <summary>
        /// Path
        /// </summary>
        public string? Path { get; set; } = "Logs";

        /// <summary>
        /// AbsolutePath
        /// </summary>
        public string AbsolutePath => System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path ?? "Logs"));

        /// <summary>
        /// AutoRecoveryLogFile
        /// </summary>
        public bool? AutoRecoveryLogFile { get; set; } = true;

        /// <summary>
        /// AutoRecoveryBeforeDay
        /// </summary>
        public int? AutoRecoveryBeforeDay { get; set; } = 7;
    }
}