using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Cleanup
{
    [UsedImplicitly]
    public class ArtifactsVariableProvider : IVariableProvider
    {
        public int Order => 2;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).GetValueOrThrow();

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

            return Task.FromResult(variables.ToImmutableArray());
        }
    }
}
