﻿using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Core.ProcessUtils
{
    public static class ProcessHelper
    {
        private const string ToolName = "[" + nameof(ProcessHelper) + "] ";

        public static Task<ExitCode> ExecuteAsync(
            string executePath,
            IEnumerable<string>? arguments = null,
            ILogger? logger = null,
            IEnumerable<KeyValuePair<string, string>>? environmentVariables = null,
            bool addProcessNameAsLogCategory = false,
            bool addProcessRunnerCategory = false,
            string? parentPrefix = null,
            CancellationToken cancellationToken = default)
        {
            ILogger usedLogger = logger ?? Logger.None;

            string executingCategory = $"[{Path.GetFileNameWithoutExtension(Path.GetFileName(executePath))}]";

            CategoryLog? toolAction = usedLogger.IsEnabled(LogEventLevel.Information) ? (message, _) => usedLogger.Information("{Tool} [{ExecutingCategory}] {Message}", ToolName, executingCategory, message) : (CategoryLog?) null;
            CategoryLog? infoAction = usedLogger.IsEnabled(LogEventLevel.Information) ? (message, _) => usedLogger.Information("{Tool} [{ExecutingCategory}] {Message}", ToolName, executingCategory, message) : (CategoryLog?) null;
            CategoryLog? errorAction = usedLogger.IsEnabled(LogEventLevel.Error) ? (message, _) => usedLogger.Error("{Tool} [{ExecutingCategory}] {Message}", ToolName, executingCategory, message) : (CategoryLog?) null;
            CategoryLog? verboseAction = usedLogger.IsEnabled(LogEventLevel.Verbose) ? (message, _) => usedLogger.Verbose("{Tool} [{ExecutingCategory}] {Message}", ToolName, executingCategory, message) : (CategoryLog?) null;
            CategoryLog? debugAction = usedLogger.IsEnabled(LogEventLevel.Debug) ? (message, _) => usedLogger.Debug("{Tool} [{ExecutingCategory}] {Message}", ToolName, executingCategory, message) : (CategoryLog?) null;

            return ProcessRunner.ExecuteProcessAsync(
                executePath,
                arguments,
                standardOutLog: infoAction,
                standardErrorAction: errorAction,
                verboseAction: verboseAction,
                toolAction: toolAction,
                environmentVariables: environmentVariables,
                debugAction: debugAction,
                cancellationToken: cancellationToken);
        }
    }
}
