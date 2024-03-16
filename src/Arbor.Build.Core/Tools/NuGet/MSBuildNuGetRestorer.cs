using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Logging;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

[Priority(101)]
[UsedImplicitly]
public class MsBuildNuGetRestorer(IFileSystem fileSystem, BuildContext buildContext) : ITool
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
        if (buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled))
        {
            return ExitCode.Success;
        }

        bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.MSBuildNuGetRestoreEnabled, true);

        if (!enabled)
        {
            logger.Debug("{Tool} is disabled", nameof(MsBuildNuGetRestorer));
            return ExitCode.Success;
        }

        var msbuildExePath = buildVariables.GetVariable(WellKnownVariables.ExternalTools_MSBuild_ExePath)
            .GetValueOrThrow().ParseAsPath();

        DirectoryEntry rootPath = buildContext.SourceRoot;

        FileEntry[] solutionFiles = rootPath.EnumerateFiles("*.sln", SearchOption.AllDirectories).ToArray();

        PathLookupSpecification pathLookupSpecification =
            DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "node_modules" });

        var excludeListStatus = solutionFiles
            .Select(file => new {File = file, Status = pathLookupSpecification.IsFileExcluded(file, rootPath)})
            .ToArray();

        FileEntry[] included = excludeListStatus
            .Where(file => !file.Status.Item1)
            .Select(file => file.File)
            .ToArray();

        var excluded = excludeListStatus
            .Where(file => file.Status.Item1)
            .ToArray();

        if (included.Length > 1)
        {
            logger.Error(
                "Expected exactly 1 solution file, found {Length}, {SolutionFiles}",
                included.Length,
                string.Join(", ", included.Select(fi => fileSystem.ConvertPathToInternal(fi.Path))));
            return ExitCode.Failure;
        }

        if (included.Length == 0)
        {
            logger.Error("Expected exactly 1 solution file, found 0");
            return ExitCode.Failure;
        }

        if (excluded.Length > 0)
        {
            logger.Debug(
                "Found ignored solution files: {IgnoredSolutionFiles}",
                string.Join(
                    ", ",
                    excluded.Select(excludedItem => $"{excludedItem.File.ConvertPathToInternal()} ({excludedItem.Status.Item2})")));
        }

        var solutionFile = included.Single();

        string? runtimeIdentifier =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.PublishRuntimeIdentifier);

        var arguments = new List<string> { solutionFile.ConvertPathToInternal(), "/t:restore" };

        if (!string.IsNullOrWhiteSpace(runtimeIdentifier))
        {
            arguments.Add($"/p:RuntimeIdentifiers={runtimeIdentifier}");
            logger.Debug("Restoring using runtime identifiers {Identifiers}", runtimeIdentifier);
        }

        ExitCode exitCode;

        List<(string Message, LogEventLevel Level)> allMessages = [];
        List<(string Message, LogEventLevel Level)> defaultMessages = [];

        using (Logger processLogger = CreateProcessLogger(logger, allMessages, defaultMessages))
        {
            exitCode = await ProcessHelper.ExecuteAsync(
                fileSystem.ConvertPathToInternal(msbuildExePath),
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