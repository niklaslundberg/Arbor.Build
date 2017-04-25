using System;

namespace Arbor.X.Core.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly string _prefix;
        private LogLevel _maxLogLevel;

        public ConsoleLogger(string prefix = "", LogLevel maxLogLevel = default(LogLevel))
        {
            _maxLogLevel = maxLogLevel;
            _prefix = prefix ?? string.Empty;
        }

        public LogLevel LogLevel
        {
            get { return _maxLogLevel; }
            set { _maxLogLevel = value; }
        }

        public void WriteDebug(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
            Console.ResetColor();
        }

        public void WriteError(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (LogLevel.Error.Level <= _maxLogLevel.Level)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
                Console.ResetColor();
            }
        }

        public void Write(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (LogLevel.Information.Level <= _maxLogLevel.Level)
            {
                Console.ResetColor();
                Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
            }
        }

        public void WriteWarning(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (LogLevel.Warning.Level <= _maxLogLevel.Level)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
                Console.ResetColor();
            }
        }

        public void WriteVerbose(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (LogLevel.Verbose.Level <= _maxLogLevel.Level)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
                Console.ResetColor();
            }
        }

        private string GetPrefix(string prefix)
        {
            string value = !string.IsNullOrWhiteSpace(prefix) ? prefix : _prefix;

            return value;
        }

        private string GetTotalMessage(string prefix, string message)
        {
            return (prefix ?? string.Empty).Trim(' ') + " " + (message ?? string.Empty).Trim(' ');
        }
    }
}