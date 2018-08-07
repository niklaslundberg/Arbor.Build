using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.Cleanup
{
    [UsedImplicitly]
    public class ArtifactsVariableProvider : IVariableProvider
    {
        public int Order => 2;

        public Task<IEnumerable<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            DirectoryInfo artifactsDirectory = new DirectoryInfo(Path.Combine(sourceRoot, "Artifacts")).EnsureExists();
            DirectoryInfo testReportsDirectory =
                new DirectoryInfo(Path.Combine(artifactsDirectory.FullName, "TestReports")).EnsureExists();

            var variables = new List<IVariable>
            {
                new BuildVariable(
                    WellKnownVariables.Artifacts,
                    artifactsDirectory.FullName),
                new BuildVariable(WellKnownVariables.ReportPath, testReportsDirectory.FullName)
            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}
