using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

namespace Arbor.X.Core.Tools.Libz
{
    public class LibZVariableProvider : IVariableProvider
    {
        public int Order { get; } = VariableProviderOrder.Ignored;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string toolsPath = buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            string exePath = Path.Combine(toolsPath, "LibZ", "LibZ.exe");

            var variables = new List<IVariable>
            {
                new EnvironmentVariable(
                    WellKnownVariables.ExternalTools_LibZ_ExePath,
                    exePath)
            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}
