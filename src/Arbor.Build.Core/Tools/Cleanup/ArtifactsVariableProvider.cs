using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.FS;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Cleanup;

[UsedImplicitly]
public class ArtifactsVariableProvider(BuildContext buildContext) : IVariableProvider
{
    public int Order => 2;

    public Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        var sourceRoot = buildContext.SourceRoot;

        DirectoryEntry artifactsDirectory = new DirectoryEntry(sourceRoot.FileSystem, UPath.Combine(sourceRoot.Path, "Artifacts")).EnsureExists();
        DirectoryEntry testReportsDirectory =
            new DirectoryEntry(sourceRoot.FileSystem, UPath.Combine(artifactsDirectory.Path, "TestReports")).EnsureExists();

        var variables = new List<IVariable>
        {
            new BuildVariable(
                WellKnownVariables.Artifacts,
                sourceRoot.FileSystem.ConvertPathToInternal(artifactsDirectory.FullName)),
            new BuildVariable(WellKnownVariables.ReportPath, sourceRoot.FileSystem.ConvertPathToInternal(testReportsDirectory.Path))
        };

        return Task.FromResult(variables.ToReadOnlyCollection());
    }
}