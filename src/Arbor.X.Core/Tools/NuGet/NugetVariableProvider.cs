using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
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

            string userSpecifiedNuGetExePath =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_NuGet_ExePath_Custom,
                    string.Empty);

            string nuGetExePath =
                await EnsureNuGetExeExistsAsync(logger, userSpecifiedNuGetExePath).ConfigureAwait(false);

            var variables = new List<IVariable>
            {
                new BuildVariable(
                    WellKnownVariables.ExternalTools_NuGet_ExePath,
                    nuGetExePath)
            };

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

                NuGetDownloadResult nuGetDownloadResult = await nuGetDownloadClient.DownloadNuGetAsync(NuGetDownloadSettings.Default, logger, httClient, _cancellationToken).ConfigureAwait(false);

                if (!nuGetDownloadResult.Succeeded)
                {
                    throw new InvalidOperationException("Could not download nuget.exe");
                }

                return nuGetDownloadResult.NuGetExePath;
            }
        }
    }
}
