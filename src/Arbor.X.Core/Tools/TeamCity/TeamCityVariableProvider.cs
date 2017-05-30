using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.TeamCity
{
    [UsedImplicitly]
    public class TeamCityVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
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
                logger.WriteWarning(
                    $"The build variable '{WellKnownVariables.TeamCity.ExternalTools_TeamCity_IsRunningInTeamCity}' is already defined");
            }
            else
            {
                variables.Add(new EnvironmentVariable(
                    WellKnownVariables.TeamCity.ExternalTools_TeamCity_IsRunningInTeamCity,
                    isRunningInTeamCity.ToString()));
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}
