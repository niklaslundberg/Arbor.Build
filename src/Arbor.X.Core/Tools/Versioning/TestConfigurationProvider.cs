using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Versioning
{
    [UsedImplicitly]
    public class TestConfigurationProvider : IVariableProvider
    {
        public int Order => BuildConfigurationProvider.ProviderOrder + 1;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool releaseConfigurationEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.ReleaseBuildEnabled, false);

            bool debugConfigurationEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.DebugBuildEnabled, false);

            var newVariables = new List<IVariable>();

            if (buildVariables.GetOptionalBooleanByKey(WellKnownVariables.RunTestsInReleaseConfigurationEnabled)
                .HasValue)
            {
                return Task.FromResult<IEnumerable<IVariable>>(Array.Empty<IVariable>());
            }

            if (releaseConfigurationEnabled && !debugConfigurationEnabled)
            {
                newVariables.Add(new EnvironmentVariable(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled,
                    "true"));

                logger.WriteDebug($"Release configuration is enabled but not debug, settings variable '{WellKnownVariables.RunTestsInReleaseConfigurationEnabled}' to true");
            }

            if (!releaseConfigurationEnabled && debugConfigurationEnabled)
            {
                newVariables.Add(new EnvironmentVariable(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled,
                    "false"));

                logger.WriteDebug($"Debug configuration is enabled but not release, settings variable '{WellKnownVariables.RunTestsInReleaseConfigurationEnabled}' to false");
            }

            return Task.FromResult<IEnumerable<IVariable>>(newVariables);
        }
    }
}
