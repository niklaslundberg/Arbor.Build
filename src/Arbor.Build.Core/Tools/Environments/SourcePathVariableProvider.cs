using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.MSBuild;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Environments
{
    [UsedImplicitly]
    public class SourcePathVariableProvider : IVariableProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly BuildContext _buildContext;

        public SourcePathVariableProvider(IFileSystem fileSystem, BuildContext buildContext)
        {
            _fileSystem = fileSystem;
            _buildContext = buildContext;
        }

        public int Order { get; } = -2;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var existingSourceRoot =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRoot)?.AsFullPath();

            var existingToolsDirectory =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools)?.AsFullPath();
            UPath sourceRoot;

            var variables = new List<IVariable>();

            if (existingSourceRoot?.FullName is {})
            {
                if (!_fileSystem.DirectoryExists(existingSourceRoot.Value))
                {
                    throw new InvalidOperationException(
                        $"The defined variable {WellKnownVariables.SourceRoot} has value set to '{existingSourceRoot}' but the directory does not exist");
                }

                sourceRoot = existingSourceRoot.Value;
            }

            if (sourceRoot.IsNull || sourceRoot.IsEmpty)
            {
                sourceRoot = _buildContext.SourceRoot.Path;
            }

            if (!existingSourceRoot.HasValue)
            {
                variables.Add(new BuildVariable(WellKnownVariables.SourceRoot, _fileSystem.ConvertPathToInternal(sourceRoot.FullName)));
            }

            var tempPath = new DirectoryEntry(_fileSystem, UPath.Combine(sourceRoot, "temp")).EnsureExists();

            variables.Add(
                new BuildVariable(
                    WellKnownVariables.TempDirectory,
                    tempPath.Path.FullName));

            if (existingToolsDirectory is null || existingToolsDirectory.Value.IsAbsolute)
            {
                var externalToolsRelativeApp =
                    new DirectoryEntry(_fileSystem, UPath.Combine(AppDomain.CurrentDomain.BaseDirectory!.AsFullPath(),
                        "tools",
                        "external"));

                if (externalToolsRelativeApp.Exists)
                {
                    variables.Add(new BuildVariable(
                        WellKnownVariables.ExternalTools,
                        externalToolsRelativeApp.Path.FullName));
                }
                else
                {
                    var externalTools =
                        new DirectoryEntry( _fileSystem, UPath.Combine(sourceRoot,
                            "build",
                            ArborConstants.ArborPackageName,
                            "tools",
                            "external")).EnsureExists();

                    variables.Add(new BuildVariable(
                        WellKnownVariables.ExternalTools,
                        externalTools.Path.FullName));
                }
            }

            return Task.FromResult(variables.ToImmutableArray());
        }
    }
}
