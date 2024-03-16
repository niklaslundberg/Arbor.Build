using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.FS;
using Arbor.Tooler;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

[UsedImplicitly]
public class NugetVariableProvider(IFileSystem fileSystem, BuildContext buildContext) : IVariableProvider
{
    private CancellationToken _cancellationToken;

    private async Task<UPath?> EnsureNuGetExeExistsAsync(ILogger logger, UPath? userSpecifiedNuGetExePath)
    {
        if (userSpecifiedNuGetExePath is {}
            && userSpecifiedNuGetExePath.Value != UPath.Empty
            && fileSystem.FileExists(userSpecifiedNuGetExePath.Value))
        {
            return userSpecifiedNuGetExePath.Value;
        }

        using var httClient = new HttpClient();
        var nuGetDownloadClient = new NuGetDownloadClient();

        var nuGetDownloadResult = await nuGetDownloadClient.DownloadNuGetAsync(
            NuGetDownloadSettings.Default,
            logger,
            httClient,
            _cancellationToken).ConfigureAwait(false);

        if (!nuGetDownloadResult.Succeeded)
        {
            throw new InvalidOperationException("Could not download nuget.exe");
        }

        return nuGetDownloadResult.NuGetExePath?.ParseAsPath();
    }

    public int Order => 3;

    public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        UPath? userSpecifiedNuGetExePath = buildVariables.GetVariableValueOrDefault(
            WellKnownVariables.ExternalTools_NuGet_ExePath_Custom)?.ParseAsPath();

        var nuGetExePath =
            await EnsureNuGetExeExistsAsync(logger, userSpecifiedNuGetExePath).ConfigureAwait(false);

        string path = nuGetExePath.HasValue
            ? fileSystem.ConvertPathToInternal(nuGetExePath.Value)
            : "";

        var variables = new List<IVariable>
        {
            new BuildVariable(WellKnownVariables.ExternalTools_NuGet_ExePath, path)
        };

        if (string.IsNullOrWhiteSpace(
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetRestoreEnabled, string.Empty)))
        {

            var pathLookupSpecification = new PathLookupSpecification();
            var packageConfigFiles = buildContext.SourceRoot
                .EnumerateFiles("packages.config", SearchOption.AllDirectories).Where(
                    file => !pathLookupSpecification.IsFileExcluded(file, buildContext.SourceRoot).Item1).ToArray();

            if (packageConfigFiles.Any())
            {
                variables.Add(new BuildVariable(WellKnownVariables.NuGetRestoreEnabled, "true"));
            }
        }

        return variables.ToImmutableArray();
    }
}