using System;

namespace Arbor.X.Core.Logging
{
    public class DelegateLogger : ILogger
    {
        readonly Action<string> _verbose;
        readonly Action<string> _error;
        readonly Action<string> _log;
        readonly Action<string> _warning;

        public DelegateLogger(Action<string> log, Action<string> warning, Action<string> error, Action<string> verbose = null)
        {
            _verbose = verbose ?? (message => {});
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

        public void WriteVerbose(string message)
        {
            _verbose(message);
        }
    }
}