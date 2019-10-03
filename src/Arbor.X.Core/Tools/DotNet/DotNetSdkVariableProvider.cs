using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Cleanup;
using JetBrains.Annotations;
using NuGet.Versioning;
using Serilog;

namespace Arbor.Build.Core.Tools.DotNet
{
    [UsedImplicitly]
    public class DotNetSdkVariableProvider : IVariableProvider
    {
        private const string MSBuildSdksPath = "MSBuildSDKsPath";

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

            string? programFilesX64 = Environment.GetEnvironmentVariable("ProgramW6432");

            if (!string.IsNullOrWhiteSpace(programFilesX64))
            {
                string programFilesX64FullPath = Path.Combine(
                    programFilesX64,
                    "dotnet",
                    "sdk");

                var directoryInfo = new DirectoryInfo(programFilesX64FullPath);

                if (directoryInfo.Exists)
                {
                    var semanticVersions = directoryInfo.GetDirectories()
                        .Select(dir =>
                            (Directory: dir,
                                HasVersion: SemanticVersion.TryParse(dir.Name, out SemanticVersion version),
                                Version: version))
                        .Where(dir => dir.HasVersion && !dir.Version.IsPrerelease)
                        .ToArray();

                    if (semanticVersions.Length > 0)
                    {
                        var version = semanticVersions.OrderByDescending(s => s.Version).First();

                        string sdksPath = Path.Combine(version.Directory.FullName, "sdks");

                        if (Directory.Exists(sdksPath))
                        {
                            return new IVariable[] { new BuildVariable(MSBuildSdksPath, sdksPath) }.ToImmutableArray();
                        }
                    }
                }
            }

            return ImmutableArray<IVariable>.Empty;
        }
    }
}