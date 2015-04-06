using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

namespace Arbor.X.Core.Tools.ILMerge
{
    public class ILMergeVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var toolsPath = buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            var ilMergePath = Path.Combine(toolsPath, "ILMerge", "ILMerge.exe");

            var variables = new List<IVariable>
                            {
                                new EnvironmentVariable(
                                    WellKnownVariables.ExternalTools_ILMerge_ExePath, ilMergePath)
                            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }

        public int Order => VariableProviderOrder.Ignored;
    }
}