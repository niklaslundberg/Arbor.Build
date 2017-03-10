using System;
using System.IO;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.IO
{
    public static class DirectoryCopy
    {
        public static async Task<ExitCode> CopyAsync(string sourceDir, string targetDir, ILogger optionalLogger = null, PathLookupSpecification pathLookupSpecificationOption = null, string rootDir = null)
        {
            var pathLookupSpecification = pathLookupSpecificationOption ?? DefaultPaths.DefaultPathLookupSpecification;

            ILogger logger = optionalLogger ?? new NullLogger();

            if (string.IsNullOrWhiteSpace(sourceDir))
            {
                throw new ArgumentNullException(nameof(sourceDir));
            }

            if (string.IsNullOrWhiteSpace(targetDir))
            {
                throw new ArgumentNullException(nameof(targetDir));
            }

            var sourceDirectory = new DirectoryInfo(sourceDir);

            if (!sourceDirectory.Exists)
            {
                throw new ArgumentException($"Source directory '{sourceDir}' does not exist");
            }

            if (pathLookupSpecification.IsBlackListed(sourceDir, rootDir))
            {
                logger.WriteDebug(
                    $"Directory '{sourceDir}' is blacklisted from specification {pathLookupSpecification.ToString()}");
                return ExitCode.Success;
            }

            new DirectoryInfo(targetDir).EnsureExists();

            foreach (FileInfo file in sourceDirectory.GetFiles())
            {
                string destFileName = Path.Combine(targetDir, file.Name);

                if (pathLookupSpecification.IsFileBlackListed(file.FullName, rootDir: rootDir, logger: optionalLogger))
                {
                    logger.WriteVerbose($"File '{file.FullName}' is blacklisted, skipping copying file");
                    continue;
                }

                logger.WriteVerbose($"Copying file '{file.FullName}' to destination '{destFileName}'");

                try
                {
                    file.CopyTo(destFileName, overwrite: true);
                }
                catch (PathTooLongException ex)
                {
                    logger.WriteError(
                        $"Could not copy file to '{destFileName}', path length is too long ({destFileName.Length})"
                        + " " + ex);
                    return ExitCode.Failure;
                }
                catch (Exception ex)
                {
                    logger.WriteError(
                        $"Could not copy file '{file.FullName}' to destination '{destFileName}'" +
                        " " + ex);
                    return ExitCode.Failure;
                }
            }

            foreach (DirectoryInfo directory in sourceDirectory.GetDirectories())
            {
                var exitCode = await CopyAsync(directory.FullName, Path.Combine(targetDir, directory.Name), pathLookupSpecificationOption: pathLookupSpecification);

                if (!exitCode.IsSuccess)
                {
                    return exitCode;
                }
            }

            return ExitCode.Success;
        }

    }
}
