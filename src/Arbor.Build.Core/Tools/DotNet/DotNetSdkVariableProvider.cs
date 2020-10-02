using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Cleanup;
using Arbor.Build.Core.Tools.NuGet;
using JetBrains.Annotations;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.DotNet
{
    [UsedImplicitly]
    public class DotNetSdkVariableProvider : IVariableProvider
    {
        private readonly IEnvironmentVariables _environmentVariables;
        private readonly IFileSystem _fileSystem;
        private const string MSBuildSdksPath = "MSBuildSDKsPath";

        public DotNetSdkVariableProvider(IEnvironmentVariables environmentVariables, IFileSystem fileSystem)
        {
            _environmentVariables = environmentVariables;
            _fileSystem = fileSystem;
        }

        public int Order => VariableProviderOrder.Ignored;

        public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string? definedValue = buildVariables.GetVariableValueOrDefault(MSBuildSdksPath, "");

            if (!string.IsNullOrWhiteSpace(definedValue))
            {
                return ImmutableArray<IVariable>.Empty;
            }

            var programFilesX64 = _environmentVariables.GetEnvironmentVariable("ProgramW6432")?.AsFullPath();

            if (programFilesX64 is null)
            {
                return ImmutableArray<IVariable>.Empty;
            }

            var programFilesX64FullPath = UPath.Combine(
                programFilesX64.Value,
                "dotnet",
                "sdk");

            var directoryEntry = new DirectoryEntry(_fileSystem, programFilesX64FullPath);

            if (directoryEntry.Exists)
            {
                var semanticVersions = directoryEntry.GetDirectories()
                    .Select(dir =>
                        (Directory: dir,
                            HasVersion: SemanticVersion.TryParse(dir.Name, out SemanticVersion version),
                            Version: version))
                    .Where(dir => dir.HasVersion && !dir.Version.IsPrerelease)
                    .ToArray();

                if (semanticVersions.Length > 0)
                {
                    var version = semanticVersions.OrderByDescending(s => s.Version).First();

                    var sdksPath = UPath.Combine(version.Directory.Path, "sdks");

                    if (_fileSystem.DirectoryExists(sdksPath))
                    {
                        return new IVariable[] { new BuildVariable(MSBuildSdksPath, sdksPath.FullName) }.ToImmutableArray();
                    }
                }
            }

            return ImmutableArray<IVariable>.Empty;
        }
    }
}