using System;
using System.IO;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.Processing;
using Serilog;
using Serilog.Core;
using Zio;

namespace Arbor.Build.Core.IO;

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

        (bool, string) isNotAllowed = pathLookupSpecification.IsNotAllowed(sourceDirectory, rootDir);
        if (isNotAllowed.Item1)
        {
            logger?.Debug(
                "Directory '{SourceDir}' is not allowed from specification {PathLookupSpecification}, {Item2}",
                sourceDirectory.ConvertPathToInternal(),
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
                    file.ConvertPathToInternal(),
                    isFileExcludeListed.Item2);
                continue;
            }

            string pathToInternal = file.FileSystem.ConvertPathToInternal(destFileName);
            logger?.Verbose("Copying file '{FullName}' to destination '{DestFileName}'",
                file.ConvertPathToInternal(),
                pathToInternal);

            try
            {
                file.CopyTo(destFileName, true);
            }
            catch (PathTooLongException ex)
            {
                logger?.Error(ex,
                    "{Message}",
                    $"Could not copy file to '{pathToInternal}', path length is too long ({pathToInternal.Length})"
                );
                return ExitCode.Failure;
            }
            catch (Exception ex)
            {
                logger?.Error(ex,
                    "{Message}",
                    $"Could not copy file '{file.ConvertPathToInternal()}' to destination '{pathToInternal}'");
                return ExitCode.Failure;
            }
        }

        foreach (DirectoryEntry directory in sourceDirectory.EnumerateDirectories())
        {
            ExitCode exitCode = await CopyAsync(
                directory, new DirectoryEntry(sourceDirectory.FileSystem,
                    UPath.Combine(targetDir.Path, directory.Name)),
                pathLookupSpecificationOption: pathLookupSpecification,
                rootDir: rootDir).ConfigureAwait(false);

            if (!exitCode.IsSuccess)
            {
                return exitCode;
            }
        }

        return ExitCode.Success;
    }
}