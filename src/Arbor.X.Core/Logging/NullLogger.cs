namespace Arbor.X.Core.Logging
{
    public class NullLogger : ILogger
    {
        public LogLevel LogLevel { get; set; }

        public void WriteError(string message, string prefix = null)
        {
        }

        public void Write(string message, string prefix = null)
        {
        }

        public void WriteWarning(string message, string prefix = null)
        {
        }

        public void WriteVerbose(string message, string prefix = null)
        {
        }

        public void WriteDebug(string message, string prefix = null)
        {
        }
    }
}
