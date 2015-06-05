using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NugetVariableProvider : IVariableProvider
    {
        CancellationToken _cancellationToken;

        public async Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            string userSpecifiedNuGetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_NuGet_ExePath_Custom, "");

            var nuGetExePath = await EnsureNuGetExeExistsAsync(logger, userSpecifiedNuGetExePath);

            var variables = new List<IVariable>
                            {
                                new EnvironmentVariable(
                                    WellKnownVariables.ExternalTools_NuGet_ExePath, nuGetExePath)
                            };

            return variables;
        }

        async Task<string> EnsureNuGetExeExistsAsync(ILogger logger, string userSpecifiedNuGetExePath, string nugetExeUri = null)
        {
            if (!string.IsNullOrWhiteSpace(userSpecifiedNuGetExePath))
            {
                var fileInfo = new FileInfo(userSpecifiedNuGetExePath);

                if (fileInfo.Name.Equals("nuget.exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (File.Exists(userSpecifiedNuGetExePath))
                    {
                        logger.Write(
                            $"Using NuGet '{userSpecifiedNuGetExePath}' from user specified variable '{WellKnownVariables.ExternalTools_NuGet_ExePath_Custom}'");
                        return userSpecifiedNuGetExePath;
                    }
                    logger.WriteWarning(
                        $"User has specified custom NuGet '{userSpecifiedNuGetExePath}' but the file does not exist, using fallback method to ensure NuGet exists");
                }
                else
                {
                    logger.WriteWarning(
                        $"User has specified custom NuGet '{userSpecifiedNuGetExePath}' but it does not have name 'nuget.exe', ignoring and using fallback method to ensure NuGet exists");
                }
            }

            logger.WriteVerbose("Using default method to ensure NuGet exists");

            var helper = new NuGetHelper(logger);
            var nuGetExePath = await helper.EnsureNuGetExeExistsAsync(nugetExeUri, _cancellationToken);

            return nuGetExePath;
        }

        public int Order => 3;
    }
}