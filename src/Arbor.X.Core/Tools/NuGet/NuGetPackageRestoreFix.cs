using System.IO;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.IO;

using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools.NuGet
{
    [UsedImplicitly]
    public class NuGetPackageRestoreFix : INuGetPackageRestoreFix
    {
        public async Task FixAsync(string packagesDirectory, ILogger logger)
        {
            string nlogDirectoryPath = Path.Combine(packagesDirectory, "NLog.3.2.0.0");

            var nlogDirectory = new DirectoryInfo(nlogDirectoryPath);

            if (nlogDirectory.Exists)
            {
                var targetDir = new DirectoryInfo(Path.Combine(packagesDirectory, "NLog.3.2.0"));

                if (!targetDir.Exists)
                {
                    logger.Debug("Copying NLog from '{FullName}' to '{FullName1}'", nlogDirectory.FullName, targetDir.FullName);
                    ExitCode exitCode = await DirectoryCopy.CopyAsync(
                        nlogDirectory.FullName,
                        targetDir.FullName,
                        logger,
                        new PathLookupSpecification()).ConfigureAwait(false);

                    if (!exitCode.IsSuccess)
                    {
                        logger.Warning("Failed to copy NLog NuGet package");
                    }
                }
            }
        }
    }
}
