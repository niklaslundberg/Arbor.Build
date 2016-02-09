using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

namespace Arbor.X.Core.Tools.ILRepack
{
    public class ILRepackVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var toolsPath = buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            var ilRepackPath = Path.Combine(toolsPath, "ILRepack", "ILRepack.exe");

            var variables = new List<IVariable>
                            {
                                new EnvironmentVariable(
                                    WellKnownVariables.ExternalTools_ILRepack_ExePath, ilRepackPath)
                            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }

        public int Order => VariableProviderOrder.Ignored;
    }
}
