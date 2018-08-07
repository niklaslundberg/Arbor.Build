using System;
using System.IO;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Serilog;

namespace Arbor.X.Core.IO
{
    public static class DirectoryCopy
    {
        public static async Task<ExitCode> CopyAsync(
            string sourceDir,
            string targetDir,
            ILogger optionalLogger = null,
            PathLookupSpecification pathLookupSpecificationOption = null,
            string rootDir = null)
        {
            PathLookupSpecification pathLookupSpecification =
                pathLookupSpecificationOption ?? DefaultPaths.DefaultPathLookupSpecification;

            ILogger logger = optionalLogger;

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

            (bool, string) isBlackListed = pathLookupSpecification.IsBlackListed(sourceDir, rootDir);
            if (isBlackListed.Item1)
            {
                logger?.Debug(
                    "Directory '{SourceDir}' is blacklisted from specification {PathLookupSpecification}, {Item2}",
                    sourceDir,
                    pathLookupSpecification,
                    isBlackListed.Item2);
                return ExitCode.Success;
            }

            new DirectoryInfo(targetDir).EnsureExists();

            foreach (FileInfo file in sourceDirectory.GetFiles())
            {
                string destFileName = Path.Combine(targetDir, file.Name);

                (bool, string) isFileBlackListed =
                    pathLookupSpecification.IsFileBlackListed(file.FullName, rootDir, logger: optionalLogger);

                if (isFileBlackListed.Item1)
                {
                    logger?.Verbose("File '{FullName}' is blacklisted, skipping copying file, {Item2}",
                        file.FullName,
                        isFileBlackListed.Item2);
                    continue;
                }

                logger?.Verbose("Copying file '{FullName}' to destination '{DestFileName}'",
                    file.FullName,
                    destFileName);

                try
                {
                    file.CopyTo(destFileName, true);
                }
                catch (PathTooLongException ex)
                {
                    logger?.Error(ex,
                        "{Message}",
                        $"Could not copy file to '{destFileName}', path length is too long ({destFileName.Length})"
                    );
                    return ExitCode.Failure;
                }
                catch (Exception ex)
                {
                    logger?.Error(ex,
                        "{Message}",
                        $"Could not copy file '{file.FullName}' to destination '{destFileName}'");
                    return ExitCode.Failure;
                }
            }

            foreach (DirectoryInfo directory in sourceDirectory.GetDirectories())
            {
                ExitCode exitCode = await CopyAsync(
                    directory.FullName,
                    Path.Combine(targetDir, directory.Name),
                    pathLookupSpecificationOption: pathLookupSpecification).ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    return exitCode;
                }
            }

            return ExitCode.Success;
        }
    }
}
