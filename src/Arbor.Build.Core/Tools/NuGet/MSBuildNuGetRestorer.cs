using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Logging;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Defensive;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Core.Tools.NuGet
{
    [Priority(101)]
    [UsedImplicitly]
    public class MsBuildNuGetRestorer : ITool
    {
        private static Logger CreateProcessLogger(
            ILogger logger,
            List<(string Message, LogEventLevel Level)> allMessages,
            List<(string Message, LogEventLevel Level)> defaultMessages) => new LoggerConfiguration()
                .WriteTo.Sink(new InMemorySink((message, level) => allMessages.Add((message, level)),
                    LogEventLevel.Verbose))
                .WriteTo.Sink(
                    new InMemorySink(
                        (message, level) => defaultMessages.Add((message, level)),
                        logger.MostVerboseLoggingCurrentLogLevel()))
                .MinimumLevel.Verbose()
                .CreateLogger();

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            logger ??= Logger.None??throw new ArgumentNullException(nameof(logger));

            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.MSBuildNuGetRestoreEnabled, true);

            if (!enabled)
            {
                logger?.Debug("{Tool} is disabled", nameof(MsBuildNuGetRestorer));
                return ExitCode.Success;
            }

            string msbuildExePath = buildVariables.GetVariable(WellKnownVariables.ExternalTools_MSBuild_ExePath)
                .ThrowIfEmptyValue().Value;

            string rootPath = buildVariables.GetVariable(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string[] solutionFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);

            PathLookupSpecification pathLookupSpecification =
                DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "node_modules" });

            var blackListStatus = solutionFiles.Select(
                file => new { File = file, Status = pathLookupSpecification.IsFileExcluded(file, rootPath) }).ToArray();

            string[] included = blackListStatus.Where(file => !file.Status.Item1).Select(file => file.File).ToArray();

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

            string solutionFile = included.Single();

            Maybe<IVariable> runtimeIdentifier =
                buildVariables.GetOptionalVariable(WellKnownVariables.PublishRuntimeIdentifier);

            var arguments = new List<string> { solutionFile, "/t:restore" };

            if (runtimeIdentifier.HasValue)
            {
                arguments.Add($"/p:RuntimeIdentifiers={runtimeIdentifier.Value.Value}");
                logger.Information("Restoring using runtime identifiers {Identifiers}", runtimeIdentifier.Value.Value);
            }

            ExitCode exitCode;

            List<(string Message, LogEventLevel Level)> allMessages = new List<(string, LogEventLevel)>();
            List<(string Message, LogEventLevel Level)> defaultMessages = new List<(string, LogEventLevel)>();

            using (Logger processLogger = CreateProcessLogger(logger, allMessages, defaultMessages))
            {
                exitCode = await ProcessHelper.ExecuteAsync(
                    msbuildExePath,
                    arguments,
                    processLogger,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    foreach ((string message, LogEventLevel level) in allMessages)
                    {
                        logger.Log(message, level);
                    }

                    logger.Error("Failed to restore NuGet packages via MSBuild");
                }
                else
                {
                    foreach ((string message, LogEventLevel level) in defaultMessages)
                    {
                        logger.Log(message, level);
                    }
                }
            }

            return exitCode;
        }
    }
}
