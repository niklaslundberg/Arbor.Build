using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Logging;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Processing;

using JetBrains.Annotations;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Core.Tools.NuGet
{
    [Priority(101)]
    [UsedImplicitly]
    public class MSBuildNuGetRestorer : ITool
    {
        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var enabled = buildVariables.GetBooleanByKey(WellKnownVariables.MSBuildNuGetRestoreEnabled, true);

            if (!enabled)
            {
                logger.Debug("{Tool} is disabled", nameof(MSBuildNuGetRestorer));
                return ExitCode.Success;
            }

            var msbuildExePath = buildVariables.GetVariable(WellKnownVariables.ExternalTools_MSBuild_ExePath)
                .ThrowIfEmptyValue().Value;

            var rootPath = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var solutionFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);

            var pathLookupSpecification =
                DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "node_modules" });

            var blackListStatus = solutionFiles.Select(
                file => new { File = file, Status = pathLookupSpecification.IsFileExcluded(file, rootPath) }).ToArray();

            var included = blackListStatus.Where(file => !file.Status.Item1).Select(file => file.File).ToArray();

            var excluded = blackListStatus.Where(file => file.Status.Item1).ToArray();

            if (included.Length > 1)
            {
                logger.Error(
                    "Expected exactly 1 solution file, found {Length}, {SolutionFiles}",
                    included.Length,
                    string.Join(", ", included));
                return ExitCode.Failure;
            }

            if (included.Length == 0)
            {
                logger.Error("Expected exactly 1 solution file, found 0");
                return ExitCode.Failure;
            }

            if (excluded.Length > 0)
            {
                logger.Warning(
                    "Found ignored solution files: {IgnoredSolutionFiles}",
                    string.Join(
                        ", ",
                        excluded.Select(excludedItem => $"{excludedItem.File} ({excludedItem.Status.Item2})")));
            }

            var solutionFile = included.Single();

            var runtimeIdentifier = buildVariables.GetOptionalVariable(WellKnownVariables.PublishRuntimeIdentifier);

            var arguments = new List<string> { solutionFile, "/t:restore" };

            if (runtimeIdentifier.HasValue)
            {
                arguments.Add($"/p:RuntimeIdentifiers={runtimeIdentifier.Value.Value}");
                logger.Information("Restoring using runtime identifiers {Identifiers}", runtimeIdentifier.Value.Value);
            }

            ExitCode exitCode;

            List<(string Message, LogEventLevel Level)> allMessages = new List<(string, LogEventLevel)>();
            List<(string Message, LogEventLevel Level)> defaultMessages = new List<(string, LogEventLevel)>();

            using (var processLogger = CreateProcessLogger(logger, allMessages, defaultMessages))
            {
                exitCode = await ProcessHelper.ExecuteAsync(
                               msbuildExePath,
                               arguments,
                               processLogger,
                               cancellationToken: cancellationToken).ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    foreach (var verboseLogMessage in allMessages)
                    {
                        logger.Log(verboseLogMessage.Message, verboseLogMessage.Level);
                    }

                    logger.Error("Failed to restore NuGet packages via MSBuild");
                }
                else
                {
                    foreach (var verboseLogMessage in defaultMessages)
                    {
                        logger.Log(verboseLogMessage.Message, verboseLogMessage.Level);
                    }
                }
            }

            return exitCode;
        }

        private static Logger CreateProcessLogger(
            ILogger logger,
            List<(string Message, LogEventLevel Level)> allMessages,
            List<(string Message, LogEventLevel Level)> defaultMessages)
        {
            return new LoggerConfiguration()
                .WriteTo.Sink(new InMemorySink((message, level) => allMessages.Add((message, level)), LogEventLevel.Verbose))
                .WriteTo.Sink(
                    new InMemorySink(
                        (message, level) => defaultMessages.Add((message, level)),
                        logger.MostVerboseLoggingCurrentLogLevel()))
                .MinimumLevel.Verbose()
                .CreateLogger();
        }
    }
}
