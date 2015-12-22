using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.ProcessUtils
{
    public static class ProcessHelper
    {
        const string ToolName = "[" + nameof(ProcessHelper) + "] ";

        public static Task<ExitCode> ExecuteAsync(
            string executePath,
            IEnumerable<string> arguments = null,
            ILogger logger = null,
            IEnumerable<KeyValuePair<string, string>> environmentVariables = null,
            CancellationToken cancellationToken = default(CancellationToken),
            bool addProcessNameAsLogCategory = false,
            bool addProcessRunnerCategory = false,
            string parentPrefix = null)
        {

            var usedLogger = logger ?? new NullLogger();

            var executingCategory = $"[{Path.GetFileNameWithoutExtension(Path.GetFileName(executePath))}]";

            return ProcessRunner.ExecuteAsync(
                executePath,
                cancellationToken,
                arguments,
                standardOutLog: (message, category) => usedLogger.Write(message, executingCategory),
                standardErrorAction: usedLogger.WriteError,
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
