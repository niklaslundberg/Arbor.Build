using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Serilog;

namespace Arbor.Build.Core.Tools.Environments
{
    public class SourcePathVariableProvider : IVariableProvider
    {
        public int Order { get; } = -2;

        public Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            string existingSourceRoot =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRoot, string.Empty);

            string existingToolsDirectory =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools, string.Empty);
            string sourceRoot;

            if (!string.IsNullOrWhiteSpace(existingSourceRoot))
            {
                if (!Directory.Exists(existingSourceRoot))
                {
                    throw new InvalidOperationException(
                        $"The defined variable {WellKnownVariables.SourceRoot} has value set to '{existingSourceRoot}' but the directory does not exist");
                }

                sourceRoot = existingSourceRoot;
            }
            else
            {
                sourceRoot = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());
            }

            DirectoryInfo tempPath = new DirectoryInfo(Path.Combine(sourceRoot, "temp")).EnsureExists();

            var variables = new List<IVariable>
            {
                new BuildVariable(
                    WellKnownVariables.TempDirectory,
                    tempPath.FullName)
            };

            if (string.IsNullOrWhiteSpace(existingSourceRoot))
            {
                variables.Add(new BuildVariable(WellKnownVariables.SourceRoot, sourceRoot));
            }

            if (string.IsNullOrWhiteSpace(existingToolsDirectory))
            {
                var externalToolsRelativeApp =
                    new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "tools",
                        "external"));

                if (externalToolsRelativeApp.Exists)
                {
                    variables.Add(new BuildVariable(
                        WellKnownVariables.ExternalTools,
                        externalToolsRelativeApp.FullName));
                }
                else
                {
                    DirectoryInfo externalTools =
                        new DirectoryInfo(Path.Combine(sourceRoot,
                            "build",
                            ArborConstants.ArborPackageName,
                            "tools",
                            "external")).EnsureExists();

                    variables.Add(new BuildVariable(
                        WellKnownVariables.ExternalTools,
                        externalTools.FullName));
                }
            }

            return Task.FromResult(variables.ToImmutableArray());
        }
    }
}
