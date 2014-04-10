using System.Collections.Generic;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Kudu
{
    public class KuduEnvironmentVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables)
        {
            var variables = new List<IVariable>();

            if (!buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Enabled))
            {
                if (KuduHelper.IsKuduAware(buildVariables))
                {
                    variables.Add(new EnvironmentVariable(WellKnownVariables.ExternalTools_Kudu_Enabled,
                        bool.TrueString));
                }
                else
                {
                    variables.Add(new EnvironmentVariable(WellKnownVariables.ExternalTools_Kudu_Enabled,
                        bool.FalseString));
                }
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}