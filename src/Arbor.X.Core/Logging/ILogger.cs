namespace Arbor.X.Core.Logging
{
    public interface ILogger
    {
        void WriteError(string message, string prefix = null);
        void Write(string message, string prefix = null);
        void WriteWarning(string message, string prefix = null);
        void WriteVerbose(string message, string prefix = null);
        LogLevel LogLevel { get; set; }
    }
}