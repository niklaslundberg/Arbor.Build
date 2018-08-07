using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.Kudu
{
    [UsedImplicitly]
    public class KuduEnvironmentVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public Task<IEnumerable<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            if (!buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Enabled))
            {
                if (KuduHelper.IsKuduAware(buildVariables))
                {
                    variables.Add(new BuildVariable(
                        WellKnownVariables.ExternalTools_Kudu_Enabled,
                        bool.TrueString));
                }
                else
                {
                    variables.Add(new BuildVariable(
                        WellKnownVariables.ExternalTools_Kudu_Enabled,
                        bool.FalseString));
                }
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}
