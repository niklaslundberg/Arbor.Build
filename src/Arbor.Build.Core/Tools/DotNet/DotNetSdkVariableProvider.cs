using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.Tools.Cleanup;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.Build.Core.Tools.Platform;
using Arbor.FS;
using JetBrains.Annotations;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.DotNet;

[UsedImplicitly]
public class DotNetSdkVariableProvider(IEnvironmentVariables environmentVariables, IFileSystem fileSystem)
    : IVariableProvider
{
    private const string MSBuildSdksPath = "MSBuildSDKsPath";

    public int Order => VariableProviderOrder.Ignored;

    public Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        string? definedValue = buildVariables.GetVariableValueOrDefault(MSBuildSdksPath, "");

        if (!string.IsNullOrWhiteSpace(definedValue))
        {
            return Task.FromResult(EnumerableOf<IVariable>.Empty);
        }

        UPath? sdkBasePath = null;

        if (PlatformHelper.IsWindows)
        {
            var programFilesX64 = environmentVariables.GetEnvironmentVariable("ProgramW6432")?.ParseAsPath();

            if (programFilesX64.HasValue)
            {
                sdkBasePath = UPath.Combine(programFilesX64.Value, "dotnet", "sdk");
            }
        }
        else if (PlatformHelper.IsLinux || PlatformHelper.IsMacOS)
        {
            string[] commonLocations = new[]
            {
                "/usr/share/dotnet/sdk",
                "/usr/local/share/dotnet/sdk",
                "/opt/dotnet/sdk"
            };

            foreach (string location in commonLocations)
            {
                var locationPath = location.ParseAsPath();
                if (fileSystem.DirectoryExists(locationPath))
                {
                    sdkBasePath = locationPath;
                    break;
                }
            }
        }

        if (!sdkBasePath.HasValue)
        {
            return Task.FromResult(EnumerableOf<IVariable>.Empty);
        }

        var directoryEntry = new DirectoryEntry(fileSystem, sdkBasePath.Value);

        if (directoryEntry.Exists)
        {
            var semanticVersions = directoryEntry.GetDirectories()
                .Select(dir =>
                    (Directory: dir,
                        HasVersion: SemanticVersion.TryParse(dir.Name, out SemanticVersion? version),
                        Version: version))
                .Where(dir => dir.HasVersion && !dir.Version!.IsPrerelease)
                .ToArray();

            if (semanticVersions.Length > 0)
            {
                (DirectoryEntry? directory, _, _) = semanticVersions.MaxBy(tuple => tuple.Version);

                var sdksPath = UPath.Combine(directory.Path, "sdks");

                if (fileSystem.DirectoryExists(sdksPath))
                {
                    return Task.FromResult(new IVariable[] { new BuildVariable(MSBuildSdksPath, fileSystem.ConvertPathToInternal(sdksPath)) }.ToReadOnlyCollection());
                }
            }
        }

        return Task.FromResult(EnumerableOf<IVariable>.Empty);
    }
}