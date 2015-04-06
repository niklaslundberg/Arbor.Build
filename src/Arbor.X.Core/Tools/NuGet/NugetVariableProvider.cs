using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Cleanup;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NugetVariableProvider : IVariableProvider
    {
        CancellationToken _cancellationToken;

        public async Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            var nuGetExePath = await EnsureNuGetExeExistsAsync(logger);
            
            var variables = new List<IVariable>
                            {
                                new EnvironmentVariable(
                                    WellKnownVariables.ExternalTools_NuGet_ExePath, nuGetExePath)
                            };

            return variables;
        }

        async Task<string> EnsureNuGetExeExistsAsync(ILogger logger)
        {
            var helper = new NuGetHelper(logger);
            var nuGetExePath = await helper.EnsureNuGetExeExistsAsync(_cancellationToken);

            return nuGetExePath;
        }

        public int Order => 3;
    }
}