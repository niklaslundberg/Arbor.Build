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
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            bool isRunningInTeamCity =
                buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_TeamCity_BuildConfigurationName,
                    defaultValue: false);

            if (buildVariables.HasKey(WellKnownVariables.ExternalTools_TeamCity_IsRunningInTeamCity))
            {
                logger.WriteWarning(
                    $"The build variable '{WellKnownVariables.ExternalTools_TeamCity_IsRunningInTeamCity}' is already defined");
            }
            else
            {
                variables.Add(new EnvironmentVariable(WellKnownVariables.ExternalTools_TeamCity_IsRunningInTeamCity, isRunningInTeamCity.ToString()));
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }

        public int Order => VariableProviderOrder.Ignored;
    }
}
