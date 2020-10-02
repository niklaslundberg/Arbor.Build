using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.MSBuild;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Cleanup
{
    [UsedImplicitly]
    public class ArtifactsVariableProvider : IVariableProvider
    {
        private readonly BuildContext _buildContext;

        public ArtifactsVariableProvider(BuildContext buildContext) => _buildContext = buildContext;

        public int Order => 2;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var sourceRoot = _buildContext.SourceRoot;

            DirectoryEntry artifactsDirectory = new DirectoryEntry(sourceRoot.FileSystem, UPath.Combine(sourceRoot.Path, "Artifacts")).EnsureExists();
            DirectoryEntry testReportsDirectory =
                new DirectoryEntry(sourceRoot.FileSystem, UPath.Combine(artifactsDirectory.Path, "TestReports")).EnsureExists();

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
