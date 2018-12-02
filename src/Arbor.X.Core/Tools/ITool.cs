using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Processing;
using Serilog;

namespace Arbor.Build.Core.Tools
{
    /// <summary>
    /// ITool represents an arbitrary tool than can execute basically anything.
    /// </summary>
    public interface ITool
    {
        /// <summary>
        /// Implementations should handle errors and write to the log with the passed ILogger instance. If it fails it should return an ExitCode with a code not 0 and.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="buildVariables"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>ExitCode, value 0 meaning success; any other value failure</returns>
        Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken);
    }

    public interface ITestRunnerTool : ITool
    {

    }
}
