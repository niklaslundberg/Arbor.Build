using System.Diagnostics;

namespace Arbor.X.Core.Logging
{
    public class DebugLogger : ILogger
    {
        readonly ILogger _logger;

        public DebugLogger(ILogger logger)
        {
            LogLevel = logger.LogLevel;
            _logger = logger;
        }

        public void WriteError(string message, string prefix = null)
        {
            if (Debugger.IsAttached && string.IsNullOrWhiteSpace(message))
            {
                Debugger.Break();
                return;
            }
            Debug.WriteLine(prefix + message, TraceEventType.Error.ToString());
            _logger.WriteError(message, prefix);
        }

        public void Write(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            Debug.WriteLine(prefix + message, TraceEventType.Information.ToString());
            _logger.Write(message, prefix);
        }

        public void WriteWarning(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            if (Debugger.IsAttached && string.IsNullOrWhiteSpace(message))
            {
                Debugger.Break();
                return;
            }
            Debug.WriteLine(prefix + message, TraceEventType.Warning.ToString());
            _logger.WriteWarning(message, prefix);
        }

        public void WriteVerbose(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            Debug.WriteLine(prefix + message, TraceEventType.Verbose.ToString());
            _logger.WriteVerbose(message, prefix);
        }

        public LogLevel LogLevel { get; set; }
        public void WriteDebug(string message, string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            Debug.WriteLine(prefix + message, "DEBUG");
            _logger.WriteDebug(message, prefix);
        }
    }
}