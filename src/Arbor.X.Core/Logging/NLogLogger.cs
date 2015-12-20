using System.Collections.Generic;
using System.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Arbor.X.Core.Logging
{
    public class NLogLogger : ILogger
    {
        readonly Logger _logger;
        readonly string _prefix;

        public NLogLogger(LogLevel logLevel, string prefix = "")
        {
            LogLevel = logLevel;
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            consoleTarget.Layout = "${message}";

            var nlogLogLevel = GetLogLevel();

            var rule1 = new LoggingRule("*", nlogLogLevel, consoleTarget);
            config.LoggingRules.Add(rule1);

            LogManager.Configuration = config;

            _logger = LogManager.GetCurrentClassLogger();

            _logger.Info($"Initialized NLog logger with level {nlogLogLevel.Name}");

            _prefix = prefix ?? "";
        }

        NLog.LogLevel GetLogLevel()
        {
            var mapping = new Dictionary<LogLevel, NLog.LogLevel>()
            {
                {LogLevel.Critical,NLog.LogLevel.Fatal},
                {LogLevel.Error,NLog.LogLevel.Error},
                {LogLevel.Warning,NLog.LogLevel.Warn},
                {LogLevel.Information,NLog.LogLevel.Info},
                {LogLevel.Verbose,NLog.LogLevel.Debug},
                {LogLevel.Debug,NLog.LogLevel.Trace}
            };

            var nlogLevel = mapping.Where(item => item.Key.Level == LogLevel.Level).Select(item => item.Value).SingleOrDefault();

            if (nlogLevel == null)
            {
                return NLog.LogLevel.Info;
            }

            return nlogLevel;
        }

        public void WriteError(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _logger.Error(GetTotalMessage(GetPrefix(prefix), message));
        }

        public void Write(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            _logger.Info(GetTotalMessage(GetPrefix(prefix), message));
        }

        public void WriteWarning(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            _logger.Warn(GetTotalMessage(GetPrefix(prefix), message));
        }

        public void WriteVerbose(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            _logger.Trace(GetTotalMessage(GetPrefix(prefix), message));
        }

        public LogLevel LogLevel { get; set; }

        public void WriteDebug(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            _logger.Debug(GetTotalMessage(GetPrefix(prefix), message));
        }

        string GetPrefix(string prefix)
        {
            string value = !string.IsNullOrWhiteSpace(prefix) ? prefix : _prefix;

            return value;
        }

        string GetTotalMessage(string prefix, string message)
        {
            return $"{(prefix ?? "").Trim(' ')} {(message ?? "").Trim(' ')}";
        }
    }
}
