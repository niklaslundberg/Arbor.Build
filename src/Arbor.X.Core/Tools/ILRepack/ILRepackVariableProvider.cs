using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Tools.Cleanup;
using Serilog;

namespace Arbor.X.Core.Tools.ILRepack
{
    public class ILRepackVariableProvider : IVariableProvider
    {
        public int Order { get; } = VariableProviderOrder.Ignored;

        public Task<IEnumerable<IVariable>> GetBuildVariablesAsync(
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

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}
