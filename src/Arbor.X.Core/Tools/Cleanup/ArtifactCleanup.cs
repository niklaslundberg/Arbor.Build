using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.Cleanup
{
    [Priority(41)]
    public class ArtifactCleanup : ITool
    {
        public Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool cleanupBeforeBuildEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.CleanupArtifactsBeforeBuildEnabled,
                    defaultValue: false);

            if (!cleanupBeforeBuildEnabled)
            {
                logger.WriteVerbose("Cleanup before build is disabled");
                return Task.FromResult(ExitCode.Success);
            }

            string artifactsPath = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;

            var artifactsDirectory = new DirectoryInfo(artifactsPath);

            if (!artifactsDirectory.Exists)
            {
                return Task.FromResult(ExitCode.Success);
            }

            logger.Write(string.Format("Artifact cleanup is enabled, removing all files and folders in '{0}'",
                artifactsDirectory.FullName));

            artifactsDirectory.DeleteIfExists();
            artifactsDirectory.Refresh();
            artifactsDirectory.EnsureExists();

            return Task.FromResult(ExitCode.Success);
        }
    }
}