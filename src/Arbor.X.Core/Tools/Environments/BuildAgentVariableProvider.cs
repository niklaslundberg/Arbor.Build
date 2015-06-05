using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

namespace Arbor.X.Core.Tools.Environments
{
    public class BuildAgentVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            ParseResult<bool> isBuildAgent =
                Environment.GetEnvironmentVariable(WellKnownVariables.IsRunningOnBuildAgent)
                    .TryParseBool(defaultValue: false);

            var variables = new List<IVariable>
                            {
                                new EnvironmentVariable(WellKnownVariables.IsRunningOnBuildAgent,
                                    isBuildAgent.ToString())
                            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }

        public int Order => VariableProviderOrder.Ignored;
    }
}