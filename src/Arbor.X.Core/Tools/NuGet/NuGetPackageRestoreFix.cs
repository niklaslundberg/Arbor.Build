using System.IO;
using System.Threading.Tasks;

using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.NuGet
{
    [UsedImplicitly]
    public class NuGetPackageRestoreFix : INuGetPackageRestoreFix
    {
        public async Task FixAsync(string packagesDirectory, ILogger logger)
        {
            var nlogDirectoryPath = Path.Combine(packagesDirectory, "NLog.3.2.0.0");

            var nlogDirectory = new DirectoryInfo(nlogDirectoryPath);

            if (nlogDirectory.Exists)
            {
                var targetDir = new DirectoryInfo(Path.Combine(packagesDirectory, "NLog.3.2.0"));

                if (!targetDir.Exists)
                {
                    logger.WriteDebug($"Copying NLog from '{nlogDirectory.FullName}' to '{targetDir.FullName}'");
                    var exitCode = await DirectoryCopy.CopyAsync(nlogDirectory.FullName, targetDir.FullName, logger, new PathLookupSpecification());

                    if (!exitCode.IsSuccess)
                    {
                        logger.WriteWarning("Failed to copy NLog NuGet package");
                    }
                }
            }
        }
    }
}
