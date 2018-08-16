using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Tools.Cleanup;
using Serilog;

namespace Arbor.X.Core.Tools.Libz
{
    public class LibZVariableProvider : IVariableProvider
    {
        public int Order { get; } = VariableProviderOrder.Ignored;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string toolsPath = buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            string exePath = Path.Combine(toolsPath, "LibZ", "LibZ.exe");

            var variables = new List<IVariable>
            {
                new BuildVariable(
                    WellKnownVariables.ExternalTools_LibZ_ExePath,
                    exePath)
            };

            return Task.FromResult(variables.ToImmutableArray());
        }
    }
}
