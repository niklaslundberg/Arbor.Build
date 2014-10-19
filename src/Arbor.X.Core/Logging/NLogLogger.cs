using System.Collections.Generic;
using System.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Arbor.X.Core.Logging
{
    public class NLogLogger : ILogger
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        readonly string _prefix;

        public NLogLogger(string prefix = "")
        {
            var config = LogManager.Configuration;

            if (config == null)
            {
                config = new LoggingConfiguration();
                LogManager.Configuration = config;
            }

            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);
            consoleTarget.Layout = "NLOG: ${message}";

            var logLevel = GetLogLevel();

            var rule1 = new LoggingRule("*", logLevel, consoleTarget);
            config.LoggingRules.Add(rule1);

            Logger.Info(string.Format("Initialized NLog logger with level {0}", logLevel.Name));

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

            var nlogLevel = mapping.Where(item => item.Key == LogLevel).Select(item => item.Value).SingleOrDefault();

            if (nlogLevel == null)
            {
                return NLog.LogLevel.Info;
            }

            return nlogLevel;
        }

        public void WriteError(string message, string prefix = null)
        {
            Logger.Error(GetTotalMessage(GetPrefix(prefix), message));
        }

        public void Write(string message, string prefix = null)
        {
            Logger.Info(GetTotalMessage(GetPrefix(prefix), message));
        }

        public void WriteWarning(string message, string prefix = null)
        {
            Logger.Warn(GetTotalMessage(GetPrefix(prefix), message));
        }

        public void WriteVerbose(string message, string prefix = null)
        {
            Logger.Trace(GetTotalMessage(GetPrefix(prefix), message));
        }

        public LogLevel LogLevel { get; set; }

        public void WriteDebug(string message, string prefix = null)
        {
            Logger.Debug(GetTotalMessage(GetPrefix(prefix), message));
        }

        string GetPrefix(string prefix)
        {
            string value = !string.IsNullOrWhiteSpace(prefix) ? prefix : _prefix;

            return value;
        }

        string GetTotalMessage(string prefix, string message)
        {
            return (prefix ?? "").Trim(' ') + " " + (message ?? "").Trim(' ');
        }
    }
}