using System;

namespace Arbor.X.Core.Logging
{
    public sealed class DelegateLogger : ILogger
    {
        private readonly Action<string, string> _debug;
        private readonly Action<string, string> _error;
        private readonly Action<string, string> _log;
        private readonly Action<string, string> _warning;
        private readonly Action<string, string> _verbose;


        public DelegateLogger(
            Action<string, string> log = null,
            Action<string, string> warning = null,
            Action<string, string> error = null,
            Action<string, string> verbose = null,
            Action<string, string> debug = null)
        {
            void Noop(string message, string prefix)
            {
            }

            _verbose = verbose ?? Noop;
            _log = log ?? Noop;
            _warning = warning ?? Noop;
            _error = error ?? Noop;
            _debug = debug ?? Noop;
        }

        public LogLevel LogLevel { get; set; }

        public void WriteError(string message, string prefix = null)
        {
            _error(message, prefix);
        }

        public void Write(string message, string prefix = null)
        {
            _log(message, prefix);
        }

        public void WriteWarning(string message, string prefix = null)
        {
            _warning(message, prefix);
        }

        public void WriteVerbose(string message, string prefix = null)
        {
            _verbose(message, prefix);
        }

        public void WriteDebug(string message, string prefix = null)
        {
            _debug(message, prefix);
        }
    }
}
