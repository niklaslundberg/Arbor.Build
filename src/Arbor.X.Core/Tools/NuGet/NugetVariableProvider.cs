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
using Arbor.Tooler;

using JetBrains.Annotations;

using Serilog;

namespace Arbor.Build.Core.Tools.NuGet
{
    [UsedImplicitly]
    public class NugetVariableProvider : IVariableProvider
    {
        private CancellationToken _cancellationToken;

        public int Order => 3;

        public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            var userSpecifiedNuGetExePath = buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_NuGet_ExePath_Custom,
                string.Empty);

            var nuGetExePath = await EnsureNuGetExeExistsAsync(logger, userSpecifiedNuGetExePath).ConfigureAwait(false);

            var variables = new List<IVariable>
                                {
                                    new BuildVariable(WellKnownVariables.ExternalTools_NuGet_ExePath, nuGetExePath)
                                };

            if (string.IsNullOrWhiteSpace(
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetRestoreEnabled, string.Empty)))
            {
                var sourceDir = buildVariables.Require(WellKnownVariables.SourceRoot).Value;

                var pathLookupSpecification = new PathLookupSpecification();
                var packageConfigFiles = new DirectoryInfo(sourceDir)
                    .GetFiles("packages.config", SearchOption.AllDirectories).Where(
                        file => !pathLookupSpecification.IsFileExcluded(file.FullName, sourceDir).Item1).ToArray();

                if (packageConfigFiles.Any())
                {
                    variables.Add(new BuildVariable(WellKnownVariables.NuGetRestoreEnabled, "true"));
                }
            }

            return variables.ToImmutableArray();
        }

        private async Task<string> EnsureNuGetExeExistsAsync(ILogger logger, string userSpecifiedNuGetExePath)
        {
            if (File.Exists(userSpecifiedNuGetExePath))
            {
                return userSpecifiedNuGetExePath;
            }

            using (var httClient = new HttpClient())
            {
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

                return nuGetDownloadResult.NuGetExePath;
            }
        }
    }
}
