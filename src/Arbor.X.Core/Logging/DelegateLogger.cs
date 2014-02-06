using System;

namespace Arbor.X.Core.Logging
{
    public class DelegateLogger : ILogger
    {
        readonly Action<string> _error;
        readonly Action<string> _log;
        readonly Action<string> _warning;

        public DelegateLogger(Action<string> log, Action<string> warning, Action<string> error)
        {
            _log = log ?? (message => { });
            _warning = warning ?? (message => { });
            _error = error ?? (message => { });
        }

        public void WriteError(string message)
        {
            _error(message);
        }

        public void Write(string message)
        {
            _log(message);
        }

        public void WriteWarning(string message)
        {
            _warning(message);
        }
    }
}