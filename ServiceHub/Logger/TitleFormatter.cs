using System.Text.RegularExpressions;

namespace ServiceHub.Logger
{
    /// <summary>
    /// TitleFormatter
    /// </summary>
    internal partial class TitleFormatter
    {
        /// <summary>
        /// TitleFormatter
        /// </summary>
        /// <param name="format"></param>
        public TitleFormatter(string format)
        {
            Formatter = format;
            Title = GetTitle();
            FormatterRegulation = GetFormatterRegulation();
        }

        /// <summary>
        /// Formatter
        /// </summary>
        public string Formatter { get; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// FormatterRegulation
        /// </summary>
        public Func<LogLevel, EventId, int, DateTimeOffset, string, string> FormatterRegulation { get; }

        private readonly string _dateDefalutFormatter = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// GetTitle
        /// </summary>
        /// <returns></returns>
        private string GetTitle()
        {
            return FormatterRegex().Replace(Formatter, c => c.Groups[1].Value);
        }

        /// <summary>
        /// GetFormatterRegulation
        /// </summary>
        /// <returns></returns>
        private Func<LogLevel, EventId, int, DateTimeOffset, string, string> GetFormatterRegulation()
        {
            return (l, e, t, d, c) => FormatterRegex().Replace(Formatter, match =>
                {
                    return match.Groups[1].Value?.ToUpper() switch
                    {
                        "DATETIME" => d.ToString(match.Groups.Count > 2 ? match.Groups[2].Value : _dateDefalutFormatter),
                        "LOGLEVEL" => l.ToString(),
                        "EVENTID" => e.ToString(),
                        "THREADID" => t.ToString(),
                        "CATEGORY" => c,
                        _ => match.Groups[1].Value,
                    };
                });
        }

        [GeneratedRegex("\\{((?:(?![\\{\\}:]).)+)(?::((?:(?![\\{\\}]).)+))*\\}", RegexOptions.IgnoreCase | RegexOptions.Multiline, "zh-CN")]
        private static partial Regex FormatterRegex();
    }
}