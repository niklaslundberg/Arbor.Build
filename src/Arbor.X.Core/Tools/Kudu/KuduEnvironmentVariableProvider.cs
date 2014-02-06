using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Kudu
{
    public class KuduEnvironmentVariableProvider : IVariableProvider
    {
        public async Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables)
        {
            var variables = new List<IVariable>();
            if (!HasKey(buildVariables, WellKnownVariables.ExternalTools_Kudu_Enabled))
            {
                if (buildVariables.Any(bv => bv.Key.StartsWith("kudu_", StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (HasKey(buildVariables, WellKnownVariables.ExternalTools_Kudu_DeploymentTarget))
                    {
                        if (HasKey(buildVariables, WellKnownVariables.ExternalTools_Kudu_Platform))
                        {
                            variables.Add(new EnvironmentVariable(WellKnownVariables.ExternalTools_Kudu_Enabled,
                                bool.TrueString));
                        }
                    }
                }
                else
                {
                    variables.Add(new EnvironmentVariable(WellKnownVariables.ExternalTools_Kudu_Enabled,
                        bool.FalseString));
                }
            }

            return variables;
        }

        static bool HasKey(IReadOnlyCollection<IVariable> buildVariables, string key)
        {
            return buildVariables.Any(
                bv => bv.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}