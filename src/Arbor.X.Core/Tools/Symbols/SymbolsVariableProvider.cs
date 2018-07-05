using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;

using Arbor.X.Core.Tools.Cleanup;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.Symbols
{
    [UsedImplicitly]
    public class SymbolsVariableProvider : IVariableProvider
    {
        public int Order => VariableProviderOrder.Ignored;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            // TODO add symbol api and key
            var variables = new List<IVariable>
            {
                new EnvironmentVariable(
                    WellKnownVariables.ExternalTools_SymbolServer_ApiKey,
                    "TODO"),
                new EnvironmentVariable(
                    WellKnownVariables.ExternalTools_SymbolServer_Uri,
                    "TODO")
            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}
