using System;

namespace Arbor.X.Core.Logging
{
    public class ConsoleLogger : ILogger
    {
        public LogLevel LogLevel
        {
            get { return _maxLogLevel; }
            set { _maxLogLevel = value; }
        }

        public void WriteDebug(string message, string prefix = null)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
            Console.ResetColor();
        }

        LogLevel _maxLogLevel;
        readonly string _prefix;

        public ConsoleLogger(string prefix = "", LogLevel maxLogLevel = default(LogLevel))
        {
            _maxLogLevel = maxLogLevel;
            _prefix = prefix ?? "";
        }

        public void WriteError(string message, string prefix = null)
        {
            if (LogLevel.Error.Level <= _maxLogLevel.Level)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
                Console.ResetColor();
            }
        }

        public void Write(string message, string prefix = null)
        {
            if (LogLevel.Information.Level <= _maxLogLevel.Level)
            {
                Console.ResetColor();
                Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
            }
        }

        public void WriteWarning(string message, string prefix = null)
        {
            if (LogLevel.Warning.Level <= _maxLogLevel.Level)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
                Console.ResetColor();
            }
        }

        public void WriteVerbose(string message, string prefix = null)
        {
            if (LogLevel.Verbose.Level <= _maxLogLevel.Level)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
                Console.ResetColor();
            }
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

        public void Write(string message, ConsoleColor color, string prefix = null)
        {
            if (LogLevel.Information.Level <= _maxLogLevel.Level)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(GetTotalMessage(GetPrefix(prefix), message));
                Console.ResetColor();
            }
        }
    }
}