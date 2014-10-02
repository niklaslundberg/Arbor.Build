using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.TeamCity
{
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
                logger.WriteWarning(string.Format("The build variable '{0}' is already defined",
                    WellKnownVariables.ExternalTools_TeamCity_IsRunningInTeamCity));
            }
            else
            {
                variables.Add(new EnvironmentVariable(WellKnownVariables.ExternalTools_TeamCity_IsRunningInTeamCity, isRunningInTeamCity.ToString()));
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}