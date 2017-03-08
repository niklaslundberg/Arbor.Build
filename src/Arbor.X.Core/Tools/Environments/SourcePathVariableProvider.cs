using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;



using Arbor.Aesculus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Environments
{
    public class SourcePathVariableProvider : IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var existingSourceRoot = buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRoot, "");
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
                sourceRoot = VcsPathHelper.FindVcsRootPath();
            }

            var externalTools =
                new DirectoryInfo(Path.Combine(sourceRoot, "build", "Arbor.X", "tools", "external")).EnsureExists();

            var tempPath = new DirectoryInfo(Path.Combine(sourceRoot, "temp")).EnsureExists();

            var variables = new List<IVariable>
                                {
                                    new EnvironmentVariable(
                                        WellKnownVariables.ExternalTools,
                                        externalTools.FullName),
                                    new EnvironmentVariable(
                                        WellKnownVariables.TempDirectory,
                                        tempPath.FullName)
                                };

            if (string.IsNullOrWhiteSpace(existingSourceRoot))
            {
                variables.Add(new EnvironmentVariable(WellKnownVariables.SourceRoot, sourceRoot));
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }

        public int Order => 0;
    }
}
