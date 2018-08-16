using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Cleanup;
using Serilog;

namespace Arbor.Build.Core.Tools.ILRepack
{
    public class ILRepackVariableProvider : IVariableProvider
    {
        public int Order { get; } = VariableProviderOrder.Ignored;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string toolsPath = buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            string ilRepackPath = Path.Combine(toolsPath, "ILRepack", "ILRepack.exe");

            var variables = new List<IVariable>
            {
                new BuildVariable(
                    WellKnownVariables.ExternalTools_ILRepack_ExePath,
                    ilRepackPath)
            };

            return Task.FromResult(variables.ToImmutableArray());
        }
    }
}
