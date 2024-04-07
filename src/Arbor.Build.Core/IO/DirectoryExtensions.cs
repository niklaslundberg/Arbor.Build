using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Arbor.Build.Core.Exceptions;
using Arbor.Build.Core.GenericExtensions;
using Zio;

namespace Arbor.Build.Core.IO;

public static class DirectoryExtensions
{
    public static ImmutableArray<FileEntry> GetFilesRecursive(
        this DirectoryEntry directory,
        IEnumerable<string>? fileExtensions = null,
        PathLookupSpecification? pathLookupSpecification = null,
        DirectoryEntry? rootDir = null)
    {
        ArgumentNullException.ThrowIfNull(directory);

        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"The directory '{directory.FullName}' does not exist");
        }

        PathLookupSpecification usedPathLookupSpecification =
            pathLookupSpecification ?? DefaultPaths.DefaultPathLookupSpecification;

        var usedFileExtensions = fileExtensions.SafeToReadOnlyCollection();

        if (usedPathLookupSpecification.IsNotAllowed(directory, rootDir).Item1)
        {
            return [];
        }

        IReadOnlyCollection<string> invalidFileExtensions = usedFileExtensions
            .Where(fileExtension => !fileExtension.StartsWith(".", StringComparison.OrdinalIgnoreCase))
            .ToReadOnlyCollection();

        if (invalidFileExtensions.Count > 0)
        {
            throw new ArgumentException(Resources.FileExtensionMustStartWithDot);
        }

        var files = new List<FileEntry>();

        var directoryFiles = directory
            .EnumerateFiles()
            .Where(file => !usedPathLookupSpecification.IsFileExcluded(file, rootDir).Item1)
            .ToList();

        var filtered = (usedFileExtensions.Any()
                ? directoryFiles.Where(file => usedFileExtensions.Any(extension => file.Path.GetExtensionWithDot()?.Equals(
                    extension,
                    StringComparison.OrdinalIgnoreCase) ?? false))
                : directoryFiles)
            .ToList();

        files.AddRange(filtered);

        DirectoryEntry[] subDirectories = directory.EnumerateDirectories().ToArray();

        files.AddRange(subDirectories
            .SelectMany(dir => dir.GetFilesRecursive(usedFileExtensions, usedPathLookupSpecification, rootDir)));

        return files.ToImmutableArray();
    }

    public static ImmutableArray<FileEntry> GetFilesWithWithExclusions(
        this DirectoryEntry directory,
        IReadOnlyCollection<string> excludedPatterns)
    {
        ArgumentNullException.ThrowIfNull(directory);

        ArgumentNullException.ThrowIfNull(excludedPatterns);

        var fileSystem = directory.FileSystem;


        var allFiles = fileSystem
            .EnumerateFileEntries(directory.Path, "*", SearchOption.AllDirectories)
            .ToArray();

        FileEntry[] allIncludedFiles;

        if (excludedPatterns.Count == 0)
        {
            allIncludedFiles = allFiles;
        }
        else
        {
            var excludedFileNames = new HashSet<FileEntry>();

            foreach (string excludedPattern in excludedPatterns.Where(pattern => pattern.Length > 0))
            {
                FileEntry[] excludedFiles;

                try
                {
                    if (excludedPattern.Contains('/', StringComparison.InvariantCulture) || excludedPattern.Contains('\\', StringComparison.InvariantCulture))
                    {
                        excludedFiles = allFiles.Where(file =>
                            {
                                string pathInvariant = excludedPattern.Replace('\\', '/');

                                string filePath = file.FullName[directory.FullName.Length..];
                                return filePath.Contains(pathInvariant, StringComparison.OrdinalIgnoreCase);
                            })
                            .ToArray();
                    }
                    else
                    {
                        excludedFiles = fileSystem.EnumerateFileEntries(
                                directory.Path, excludedPattern,
                                SearchOption.AllDirectories)
                            .ToArray();
                    }
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    throw new InvalidOperationException($"Could not get files with pattern '{excludedPattern}'");
                }

                foreach (var excludedFile in excludedFiles)
                {
                    excludedFileNames.Add(excludedFile);
                }
            }

            allIncludedFiles = allFiles
                .Except(excludedFileNames)
                .ToArray();

            foreach (var file in allIncludedFiles)
            {
                if (!fileSystem.FileExists(file.Path))
                {
                    throw new InvalidOperationException($"The file '{file}' does not exist");
                }
            }
        }

        return allIncludedFiles.ToImmutableArray();
    }
}