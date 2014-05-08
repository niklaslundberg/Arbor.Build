using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Environments
{
    public class BuildServerVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            ParseResult<bool> isBuildServer = Environment.GetEnvironmentVariable(WellKnownVariables.IsRunningOnBuildAgent).TryParseBool(defaultValue: false);
           
            var variables = new List<IVariable>
                            {
                                new EnvironmentVariable(WellKnownVariables.IsRunningOnBuildAgent,isBuildServer.ToString())
                            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}