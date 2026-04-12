using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Cleanup;
using Arbor.Build.Core.Tools.Platform;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.DotNet;

[UsedImplicitly]
public class DotNetEnvironmentVariableProvider(IEnvironmentVariables environmentVariables, IFileSystem fileSystem)
    : IVariableProvider
{
    public int Order => VariableProviderOrder.Ignored;

    public async Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        UPath? dotNetExePath =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath)?.ParseAsPath();

        if (dotNetExePath.HasValue && dotNetExePath.Value != UPath.Empty)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(dotNetExePath?.FullName))
        {
            dotNetExePath = await FindDotNetExecutableAsync(logger, cancellationToken);
        }
        else if (!fileSystem.FileExists(dotNetExePath.Value))
        {
            logger.Warning(
                "The specified path to dotnet executable from variable '{DotNetExePath}' is set to '{DotNetExePath1}' but the file does not exist",
                WellKnownVariables.DotNetExePath,
                fileSystem.ConvertPathToInternal(dotNetExePath.Value));
            return [];
        }

        return [new BuildVariable(WellKnownVariables.DotNetExePath, string.IsNullOrWhiteSpace(dotNetExePath?.FullName) ? "" : fileSystem.ConvertPathToInternal(dotNetExePath.Value))];
    }

    private async Task<UPath?> FindDotNetExecutableAsync(ILogger logger, CancellationToken cancellationToken)
    {
        var sb = new List<string>(10);

        if (PlatformHelper.IsWindows)
        {
            var winDir = environmentVariables.GetEnvironmentVariable("WINDIR")?.ParseAsPath();

            if (winDir is null)
            {
                logger.Warning("Error finding Windows directory");
                return await TryFindInPathAsync(logger, cancellationToken);
            }

            var whereExePath = UPath.Combine(winDir.Value, "System32", "where.exe");

            ExitCode exitCode = await Processing.ProcessRunner.ExecuteProcessAsync(
                fileSystem.ConvertPathToInternal(whereExePath),
                arguments: ["dotnet.exe"],
                standardOutLog: (message, _) => sb.Add(message),
                cancellationToken: cancellationToken);

            if (!exitCode.IsSuccess)
            {
                logger.Warning("Failed to find dotnet.exe with where.exe");
                return await TryFindInPathAsync(logger, cancellationToken);
            }

            return sb.FirstOrDefault(item => item.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))?.Trim().ParseAsPath();
        }
        else if (PlatformHelper.IsLinux || PlatformHelper.IsMacOS)
        {
            ExitCode exitCode = await Processing.ProcessRunner.ExecuteProcessAsync(
                "/usr/bin/which",
                arguments: ["dotnet"],
                standardOutLog: (message, _) => sb.Add(message),
                cancellationToken: cancellationToken);

            if (!exitCode.IsSuccess)
            {
                logger.Warning("Failed to find dotnet with which command");
                return TryFindInCommonLocations(logger);
            }

            string? dotnetPath = sb.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(dotnetPath))
            {
                return dotnetPath!.ParseAsPath();
            }
            return null;
        }

        return null;
    }

    private async Task<UPath?> TryFindInPathAsync(ILogger logger, CancellationToken cancellationToken)
    {
        string? pathVariable = environmentVariables.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return null;
        }

        char pathSeparator = PlatformHelper.IsWindows ? ';' : ':';
        string[] paths = pathVariable.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries);
        string dotnetExeName = PlatformHelper.GetExecutableName("dotnet");

        foreach (string path in paths)
        {
            var dotnetPath = UPath.Combine(path.ParseAsPath(), dotnetExeName);
            if (fileSystem.FileExists(dotnetPath))
            {
                logger.Debug("Found dotnet executable in PATH at '{DotnetPath}'", fileSystem.ConvertPathToInternal(dotnetPath));
                return dotnetPath;
            }
        }

        return null;
    }

    private UPath? TryFindInCommonLocations(ILogger logger)
    {
        string[] commonLocations = new[]
        {
            "/usr/bin/dotnet",
            "/usr/local/bin/dotnet",
            "/usr/share/dotnet/dotnet",
            "/opt/dotnet/dotnet"
        };

        foreach (string location in commonLocations)
        {
            var dotnetPath = location.ParseAsPath();
            if (fileSystem.FileExists(dotnetPath))
            {
                logger.Debug("Found dotnet executable at '{DotnetPath}'", location);
                return dotnetPath;
            }
        }

        return null;
    }
}