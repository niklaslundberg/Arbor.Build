using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Artifacts
{
    public class ArtifactsVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables)
        {
            var sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var artifactsDirectory = new DirectoryInfo(Path.Combine(sourceRoot, "Artifacts")).EnsureExists();
            var testReportsDirectory = new DirectoryInfo(Path.Combine(artifactsDirectory.FullName, "TestReports")).EnsureExists();

            var variables = new List<IVariable>
                            {
                                new EnvironmentVariable(WellKnownVariables.Artifacts,
                                    artifactsDirectory.FullName),
                                new EnvironmentVariable(WellKnownVariables.ReportPath, testReportsDirectory.FullName)
                            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}