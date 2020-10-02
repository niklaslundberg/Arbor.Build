using System;
using System.IO;
using System.Threading.Tasks;
using Arbor.Processing;
using Serilog;
using Serilog.Core;
using Zio;

namespace Arbor.Build.Core.IO
{
    public static class DirectoryCopy
    {
        public static async Task<ExitCode> CopyAsync(
            DirectoryEntry sourceDirectory,
            DirectoryEntry targetDir,
            ILogger? optionalLogger = null,
            PathLookupSpecification? pathLookupSpecificationOption = null,
            DirectoryEntry? rootDir = null)
        {
            PathLookupSpecification pathLookupSpecification =
                pathLookupSpecificationOption ?? DefaultPaths.DefaultPathLookupSpecification;

            ILogger logger = optionalLogger ?? Logger.None;

            if (sourceDirectory is null)
            {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            if (targetDir is null)
            {
                throw new ArgumentNullException(nameof(targetDir));
            }

            if (!sourceDirectory.Exists)
            {
                throw new ArgumentException($"Source directory '{sourceDirectory}' does not exist");
            }

            (bool, string) isNotAllowed = pathLookupSpecification.IsNotAllowed(sourceDirectory, rootDir);
            if (isNotAllowed.Item1)
            {
                logger?.Debug(
                    "Directory '{SourceDir}' is notallowed from specification {PathLookupSpecification}, {Item2}",
                    sourceDirectory,
                    pathLookupSpecification,
                    isNotAllowed.Item2);
                return ExitCode.Success;
            }

            targetDir.EnsureExists();

            foreach (FileEntry file in sourceDirectory.EnumerateFiles())
            {
                var destFileName = UPath.Combine(targetDir.Path, file.Name);

                (bool, string) isFileExcludeListed =
                    pathLookupSpecification.IsFileExcluded(file, rootDir, logger: optionalLogger);

                if (isFileExcludeListed.Item1)
                {
                    logger?.Verbose("File '{FullName}' is not allowed, skipping copying file, {Item2}",
                        file.FullName,
                        isFileExcludeListed.Item2);
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
                        $"Could not copy file to '{destFileName}', path length is too long ({destFileName.FullName.Length})"
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

            foreach (DirectoryEntry directory in sourceDirectory.EnumerateDirectories())
            {
                ExitCode exitCode = await CopyAsync(
                    directory, new DirectoryEntry(sourceDirectory.FileSystem,
                    UPath.Combine(targetDir.Path, directory.Name)),
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
