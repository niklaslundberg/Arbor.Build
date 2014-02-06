namespace Arbor.X.Core.Logging
{
    public interface ILogger
    {
        void WriteError(string message);
        void Write(string message);
        void WriteWarning(string message);
    }
}