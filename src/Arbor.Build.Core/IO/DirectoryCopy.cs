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
            var fileSystem = sourceDirectory.FileSystem;
            PathLookupSpecification pathLookupSpecification =
                pathLookupSpecificationOption ?? DefaultPaths.DefaultPathLookupSpecification;

            ILogger logger = optionalLogger ?? Logger.None;

            (bool, string) isNotAllowed = pathLookupSpecification.IsNotAllowed(sourceDirectory, rootDir);
            if (isNotAllowed.Item1)
            {
                logger?.Debug(
                    "Directory '{SourceDir}' is not allowed from specification {PathLookupSpecification}, {Item2}",
                    fileSystem.ConvertPathToInternal(sourceDirectory.Path),
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
                        fileSystem.ConvertPathToInternal(file.Path),
                        isFileExcludeListed.Item2);
                    continue;
                }

                logger?.Verbose("Copying file '{FullName}' to destination '{DestFileName}'",
                    fileSystem.ConvertPathToInternal(file.Path),
                    fileSystem.ConvertPathToInternal(destFileName));

                try
                {
                    file.CopyTo(destFileName, true);
                }
                catch (PathTooLongException ex)
                {
                    logger?.Error(ex,
                        "{Message}",
                        $"Could not copy file to '{fileSystem.ConvertPathToInternal(destFileName)}', path length is too long ({fileSystem.ConvertPathToInternal(destFileName).Length})"
                    );
                    return ExitCode.Failure;
                }
                catch (Exception ex)
                {
                    logger?.Error(ex,
                        "{Message}",
                        $"Could not copy file '{fileSystem.ConvertPathToInternal(file.Path)}' to destination '{fileSystem.ConvertPathToInternal(destFileName)}'");
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
