namespace Arbor.X.Core.Logging
{
    public interface ILogger
    {
        LogLevel LogLevel { get; set; }

        void WriteError(string message, string prefix = null);

        void Write(string message, string prefix = null);

        void WriteWarning(string message, string prefix = null);

        void WriteVerbose(string message, string prefix = null);

        void WriteDebug(string message, string prefix = null);
    }
}
