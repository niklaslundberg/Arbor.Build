using System.Collections.Generic;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NugetVariableProvider : IVariableProvider
    {
        public async Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables)
        {
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
            var nuGetExePath = await helper.EnsureNuGetExeExistsAsync();

            return nuGetExePath;
        }
    }
}