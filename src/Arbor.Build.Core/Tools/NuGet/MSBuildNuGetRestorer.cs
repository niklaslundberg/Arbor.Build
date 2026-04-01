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
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Core.Tools.Platform;
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
        // Skip on non-Windows platforms - MSBuild is Windows-only
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            logger.Debug(
                "{Tool} is skipped on non-Windows platform ({Platform}). Use DotNetRestorer instead",
                nameof(MsBuildNuGetRestorer),
                Environment.OSVersion.Platform);
            return ExitCode.Success;
        }

        if (buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled))
        {
            logger.Debug(
                "{Tool} is skipped because {Variable} is enabled (use dotnet restore instead)",
                nameof(MsBuildNuGetRestorer),
                WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled);
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

        FileEntry[] solutionFiles = rootPath.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(file => MsBuildConstants.SolutionFileSearchPattern.Any(pattern => pattern.Equals(file.Path.GetExtensionWithDot())))
            .ToArray();

        PathLookupSpecification pathLookupSpecification =
            DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(["node_modules"]);

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

        // Prefer .sln over .slnx for MSBuild compatibility
        // .slnx is modern Visual Studio format but may not be supported by all MSBuild versions
        var solutionFile = included
            .OrderBy(f => f.Path.GetExtensionWithDot().Equals(".slnx", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .First();

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
                cancellationToken: cancellationToken);

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