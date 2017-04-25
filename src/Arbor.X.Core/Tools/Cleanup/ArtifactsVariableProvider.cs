using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Cleanup
{
    [UsedImplicitly]
    public class ArtifactsVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
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
                new EnvironmentVariable(
                    WellKnownVariables.Artifacts,
                    artifactsDirectory.FullName),
                new EnvironmentVariable(WellKnownVariables.ReportPath, testReportsDirectory.FullName)
            };

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }

        public int Order => 2;
    }
}
