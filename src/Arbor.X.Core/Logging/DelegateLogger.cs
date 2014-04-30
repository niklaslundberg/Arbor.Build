using System;

namespace Arbor.X.Core.Logging
{
    public sealed class DelegateLogger : ILogger
    {
        readonly Action<string, string> _verbose;
        readonly Action<string, string> _error;
        readonly Action<string, string> _log;
        readonly Action<string, string> _warning;

        public DelegateLogger(Action<string, string> log, Action<string, string> warning, Action<string, string> error, Action<string, string> verbose = null)
        {
            _verbose = verbose ?? ((message, prefix) => {});
            _log = log ?? ((message, prefix) => { });
            _warning = warning ?? ((message, prefix) => { });
            _error = error ?? ((message, prefix) => { });
        }

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
    }
}