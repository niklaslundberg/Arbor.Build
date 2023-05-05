using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.ProcessUtils;
using Arbor.Build.Core.Tools.EnvironmentVariables;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

[Priority(52)]
[UsedImplicitly]
public class NuGetEnvironmentVerification : EnvironmentVerification
{
    private readonly IFileSystem _fileSystem;
    public NuGetEnvironmentVerification(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        RequiredValues.Add(WellKnownVariables.ExternalTools_NuGet_ExePath);
    }

    protected override async Task<bool> PostVariableVerificationAsync(
        StringBuilder variableBuilder,
        IReadOnlyCollection<IVariable> buildVariables,
        ILogger logger)
    {
        IVariable? variable =
            buildVariables.SingleOrDefault(item => item.Key == WellKnownVariables.ExternalTools_NuGet_ExePath);

        if (variable?.Value is null)
        {
            return false;
        }

        UPath nuGetExePath = variable.Value.ParseAsPath();

        bool fileExists = _fileSystem.FileExists(nuGetExePath);

        if (!fileExists)
        {
            variableBuilder.Append("NuGet.exe path '").Append(nuGetExePath).AppendLine("' does not exist");
        }
        else
        {
            bool nuGetUpdateEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetSelfUpdateEnabled);

            if (nuGetUpdateEnabled)
            {
                logger.Verbose("NuGet self update is enabled by variable '{NuGetSelfUpdateEnabled}'",
                    WellKnownVariables.NuGetSelfUpdateEnabled);

                await EnsureMinNuGetVersionAsync(nuGetExePath, logger).ConfigureAwait(false);
            }
            else
            {
                logger.Verbose("NuGet self update is disabled by variable '{NuGetSelfUpdateEnabled}'",
                    WellKnownVariables.NuGetSelfUpdateEnabled);
            }
        }

        return fileExists;
    }

    private async Task EnsureMinNuGetVersionAsync(UPath nuGetExePath, ILogger logger)
    {
        var standardOut = new List<string>();
        ILogger versionLogger = InMemoryLoggerHelper.CreateInMemoryLogger((message, level) => standardOut.Add(message));

        try
        {
            IEnumerable<string> args = new List<string>();
            ExitCode versionExitCode = await ProcessHelper.ExecuteAsync(_fileSystem.ConvertPathToInternal(nuGetExePath), args, versionLogger)
                .ConfigureAwait(false);

            if (!versionExitCode.IsSuccess)
            {
                logger.Warning("NuGet version exit code was {VersionExitCode}", versionExitCode);
                return;
            }

            const string nugetVersion = "NuGet Version: ";
            string? versionLine =
                standardOut.FirstOrDefault(
                    line => line.StartsWith(nugetVersion, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(versionLine))
            {
                logger.Warning("Could not ensure NuGet version, no version line in NuGet output");
                return;
            }

            char majorNuGetVersion = versionLine[nugetVersion.Length..].FirstOrDefault();

            if (majorNuGetVersion == '2')
            {
                IEnumerable<string> updateSelfArgs = new List<string> { "update", "-self" };
                ExitCode exitCode = await ProcessHelper.ExecuteAsync(_fileSystem.ConvertPathToInternal(nuGetExePath), updateSelfArgs, logger)
                    .ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    logger.Warning("The NuGet version could not be determined, exit code {ExitCode}", exitCode);
                }

                return;
            }

            if (majorNuGetVersion != '3')
            {
                logger.Warning(
                    "NuGet version could not be determined, major version starts with character {MajorNuGetVersion}",
                    majorNuGetVersion);
                return;
            }

            logger.Verbose("NuGet major version is {MajorNuGetVersion}", majorNuGetVersion);
        }
        finally
        {
            if (versionLogger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}