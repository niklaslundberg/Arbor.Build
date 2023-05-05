using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Environments;

[UsedImplicitly]
public class BuildAgentVariableProvider : IVariableProvider
{
    public int Order => VariableProviderOrder.Default;

    public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        bool isBuildAgentValue;

        var buildAgentEnvironmentVariables = new List<string>
        {
            WellKnownVariables.ExternalTools_Hudson_HudsonHome,
            WellKnownVariables.ExternalTools_Jenkins_JenkinsHome,
            WellKnownVariables.ExternalTools_TeamCity_TeamCityVersion
        };

        bool isBuildAgent = buildVariables.GetBooleanByKey(WellKnownVariables.IsRunningOnBuildAgent);

        if (isBuildAgent)
        {
            isBuildAgentValue = true;
        }
        else
        {
            isBuildAgentValue =
                buildAgentEnvironmentVariables.Any(
                    buildAgent => buildVariables.GetOptionalBooleanByKey(buildAgent) == true);
        }

        var variables = new List<IVariable>
        {
            new BuildVariable(
                WellKnownVariables.IsRunningOnBuildAgent,
                isBuildAgentValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant())
        };

        return Task.FromResult(variables.ToImmutableArray());
    }
}