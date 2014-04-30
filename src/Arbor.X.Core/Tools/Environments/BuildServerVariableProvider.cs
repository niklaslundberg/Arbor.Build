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
            var isBuildServerEnvironmentVariable = Environment.GetEnvironmentVariable("IsBuildServer");
            bool isBuildServer = false;

            if (!string.IsNullOrWhiteSpace(isBuildServerEnvironmentVariable))
            {
                bool isBuildServerValue;
                isBuildServer = bool.TryParse(isBuildServerEnvironmentVariable, out isBuildServerValue) &&
                                     isBuildServerValue;

            }
            var variables = new List<IVariable>
                            {
                                new EnvironmentVariable(WellKnownVariables.IsRunningOnBuildAgent,isBuildServer.ToString())
                            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}