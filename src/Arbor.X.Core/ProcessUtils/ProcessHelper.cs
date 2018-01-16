using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.ProcessUtils
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
            ILogger usedLogger = logger ?? new NullLogger();

            string executingCategory = $"[{Path.GetFileNameWithoutExtension(Path.GetFileName(executePath))}]";

            return ProcessRunner.ExecuteAsync(
                executePath,
                cancellationToken,
                arguments,
                (message, category) => usedLogger.Write(message, executingCategory),
                usedLogger.WriteError,
                verboseAction: usedLogger.WriteVerbose,
                toolAction: (message, category) => usedLogger.Write(message, ToolName),
                environmentVariables: environmentVariables,
                debugAction: usedLogger.WriteDebug,
                addProcessNameAsLogCategory: addProcessNameAsLogCategory,
                addProcessRunnerCategory: addProcessRunnerCategory,
                parentPrefix: parentPrefix);
        }
    }
}
