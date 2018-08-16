using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.ProcessUtils
{
    public static class ProcessHelper
    {
        private const string ToolName = "[" + nameof(ProcessHelper) + "] ";

        public static Task<ExitCode> ExecuteAsync(
            string executePath,
            IEnumerable<string> arguments = null,
            ILogger logger = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            CancellationToken cancellationToken = default,
            bool addProcessNameAsLogCategory = false,
            bool addProcessRunnerCategory = false,
            string parentPrefix = null)
        {
            ILogger usedLogger = logger ?? Logger.None;

            string executingCategory = $"[{Path.GetFileNameWithoutExtension(Path.GetFileName(executePath))}]";

            return ProcessRunner.ExecuteAsync(
                executePath,
                cancellationToken,
                arguments,
                (message, category) => usedLogger.Information(message, executingCategory),
                usedLogger.Error,
                verboseAction: usedLogger.Verbose,
                toolAction: (message, category) => usedLogger.Information(message, ToolName),
                environmentVariables: environmentVariables,
                debugAction: usedLogger.Debug,
                addProcessNameAsLogCategory: addProcessNameAsLogCategory,
                addProcessRunnerCategory: addProcessRunnerCategory,
                parentPrefix: parentPrefix);
        }
    }
}
