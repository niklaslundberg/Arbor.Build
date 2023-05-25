namespace Arbor.Build.Core.Tools;

public interface IReportLogTail
{
    FixedSizedQueue<string> LogTail { get; }
}