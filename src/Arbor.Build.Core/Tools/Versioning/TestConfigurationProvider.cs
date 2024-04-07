using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Versioning;

[UsedImplicitly]
public class TestConfigurationProvider : IVariableProvider
{
    public int Order => BuildConfigurationProvider.ProviderOrder + 1;

    public Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        bool releaseConfigurationEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.ReleaseBuildEnabled);

        bool debugConfigurationEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.DebugBuildEnabled);

        var newVariables = new List<IVariable>();

        if (buildVariables.GetOptionalBooleanByKey(WellKnownVariables.RunTestsInReleaseConfigurationEnabled)
            .HasValue)
        {
            return Task.FromResult(EnumerableOf<IVariable>.Empty);
        }

        if (releaseConfigurationEnabled && !debugConfigurationEnabled)
        {
            newVariables.Add(new BuildVariable(
                WellKnownVariables.RunTestsInReleaseConfigurationEnabled,
                "true"));

            logger.Debug(
                "Release configuration is enabled but not debug, settings variable '{RunTestsInReleaseConfigurationEnabled}' to true",
                WellKnownVariables.RunTestsInReleaseConfigurationEnabled);
        }

        if (!releaseConfigurationEnabled && debugConfigurationEnabled)
        {
            newVariables.Add(new BuildVariable(
                WellKnownVariables.RunTestsInReleaseConfigurationEnabled,
                "false"));

            logger.Debug(
                "Debug configuration is enabled but not release, settings variable '{RunTestsInReleaseConfigurationEnabled}' to false",
                WellKnownVariables.RunTestsInReleaseConfigurationEnabled);
        }

        return Task.FromResult(newVariables.ToReadOnlyCollection());
    }
}