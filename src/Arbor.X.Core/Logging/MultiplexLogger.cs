using System.Collections.Generic;
using Arbor.Defensive.Collections;

namespace Arbor.X.Core.Logging
{
    public class MultiplexLogger : ILogger
    {
        private readonly IReadOnlyCollection<ILogger> _loggers;

        public MultiplexLogger(IEnumerable<ILogger> loggers = null)
        {
            _loggers = loggers.SafeToReadOnlyCollection();
        }

        public void WriteError(string message, string prefix = null)
        {
            foreach (ILogger logger in _loggers)
            {
                logger.WriteError(message, prefix);
            }
        }

        public void Write(string message, string prefix = null)
        {
            foreach (ILogger logger in _loggers)
            {
                logger.Write(message, prefix);
            }
        }

        public void WriteWarning(string message, string prefix = null)
        {
            foreach (ILogger logger in _loggers)
            {
                logger.WriteWarning(message, prefix);
            }
        }

        public void WriteVerbose(string message, string prefix = null)
        {
            foreach (ILogger logger in _loggers)
            {
                logger.WriteVerbose(message, prefix);
            }
        }

        public LogLevel LogLevel { get; set; }

        public void WriteDebug(string message, string prefix = null)
        {
            foreach (ILogger logger in _loggers)
            {
                logger.WriteDebug(message, prefix);
            }
        }
    }
}
