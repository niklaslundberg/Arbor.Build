using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
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

        private async Task<string> EnsureNuGetExeExistsAsync(
            ILogger logger,
            string userSpecifiedNuGetExePath,
            string nugetExeUri = null)
        {
            if (!string.IsNullOrWhiteSpace(userSpecifiedNuGetExePath))
            {
                var fileInfo = new FileInfo(userSpecifiedNuGetExePath);

                if (fileInfo.Name.Equals("nuget.exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (File.Exists(userSpecifiedNuGetExePath))
                    {
                        logger.Information(
                            "Using NuGet '{UserSpecifiedNuGetExePath}' from user specified variable '{ExternalTools_NuGet_ExePath_Custom}'",
                            userSpecifiedNuGetExePath,
                            WellKnownVariables.ExternalTools_NuGet_ExePath_Custom);
                        return userSpecifiedNuGetExePath;
                    }

                    logger.Warning(
                        "User has specified custom NuGet '{UserSpecifiedNuGetExePath}' but the file does not exist, using fallback method to ensure NuGet exists",
                        userSpecifiedNuGetExePath);
                }
                else
                {
                    logger.Warning(
                        "User has specified custom NuGet '{UserSpecifiedNuGetExePath}' but it does not have name 'nuget.exe', ignoring and using fallback method to ensure NuGet exists",
                        userSpecifiedNuGetExePath);
                }
            }

            logger.Verbose("Using default method to ensure NuGet exists");

            var helper = new NuGetHelper(logger);
            string nuGetExePath = await helper.EnsureNuGetExeExistsAsync(nugetExeUri, _cancellationToken)
                .ConfigureAwait(false);

            return nuGetExePath;
        }
    }
}
