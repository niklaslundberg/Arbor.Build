using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.TeamCity
{
    [UsedImplicitly]
    public class TeamCityVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            bool isRunningInTeamCity =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.TeamCity.ExternalTools_TeamCity_BuildConfigurationName,
                    false);

            if (buildVariables.HasKey(WellKnownVariables.TeamCity.ExternalTools_TeamCity_IsRunningInTeamCity))
            {
                logger.Warning("The build variable '{ExternalTools_TeamCity_IsRunningInTeamCity}' is already defined",
                    WellKnownVariables.TeamCity.ExternalTools_TeamCity_IsRunningInTeamCity);
            }
            else
            {
                variables.Add(new BuildVariable(
                    WellKnownVariables.TeamCity.ExternalTools_TeamCity_IsRunningInTeamCity,
                    isRunningInTeamCity.ToString()));
            }

            return Task.FromResult(variables.ToImmutableArray());
        }
    }
}
