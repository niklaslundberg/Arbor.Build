using System;
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

namespace Arbor.Build.Core.Tools.Environments;

[UsedImplicitly]
public class SourcePathVariableProvider(IFileSystem fileSystem, BuildContext buildContext) : IVariableProvider
{
    public int Order { get; } = -2;

    public Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        var existingSourceRoot =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRoot)?.ParseAsPath();

        var existingToolsDirectory =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools)?.ParseAsPath();
        UPath sourceRoot = default;

        var variables = new List<IVariable>();

        if (existingSourceRoot?.FullName is {})
        {
            if (!fileSystem.DirectoryExists(existingSourceRoot.Value))
            {
                throw new InvalidOperationException(
                    $"The defined variable {WellKnownVariables.SourceRoot} has value set to '{fileSystem.ConvertPathToInternal(existingSourceRoot.Value)}' but the directory does not exist");
            }

            sourceRoot = existingSourceRoot.Value;
        }

        if (sourceRoot.IsNull || sourceRoot.IsEmpty)
        {
            sourceRoot = buildContext.SourceRoot.Path;
        }

        if (!existingSourceRoot.HasValue)
        {
            variables.Add(new BuildVariable(WellKnownVariables.SourceRoot, fileSystem.ConvertPathToInternal(sourceRoot)));
        }

        var tempPath = new DirectoryEntry(fileSystem, sourceRoot / "temp").EnsureExists();

        variables.Add(
            new BuildVariable(
                WellKnownVariables.TempDirectory,
                fileSystem.ConvertPathToInternal(tempPath.Path)));

        if (existingToolsDirectory is null || existingToolsDirectory.Value.IsAbsolute)
        {
            var externalToolsRelativeApp =
                new DirectoryEntry(fileSystem, UPath.Combine(AppContext.BaseDirectory!.ParseAsPath(),
                    "tools",
                    "external"));

            if (externalToolsRelativeApp.Exists)
            {
                variables.Add(new BuildVariable(
                    WellKnownVariables.ExternalTools,
                    fileSystem.ConvertPathToInternal(externalToolsRelativeApp.Path)));
            }
            else
            {
                var externalTools =
                    new DirectoryEntry( fileSystem, UPath.Combine(sourceRoot,
                        "build",
                        ArborConstants.ArborPackageName,
                        "tools",
                        "external")).EnsureExists();

                variables.Add(new BuildVariable(
                    WellKnownVariables.ExternalTools,
                    fileSystem.ConvertPathToInternal(externalTools.Path)));
            }
        }

        return Task.FromResult(variables.ToReadOnlyCollection());
    }
}