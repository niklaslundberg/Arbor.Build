using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Arbor.Defensive.Collections;
using Arbor.Exceptions;
using JetBrains.Annotations;
using Zio;

namespace Arbor.Build.Core.IO
{
    public static class DirectoryExtensions
    {
        public static DirectoryInfo EnsureExists(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
            {
                throw new ArgumentNullException(nameof(directoryInfo));
            }

            try
            {
                directoryInfo.Refresh();

                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
            }
            catch (PathTooLongException ex)
            {
                throw new PathTooLongException(
                    $"Could not create directory '{directoryInfo.FullName}', path length {directoryInfo.FullName.Length}",
                    ex);
            }

            directoryInfo.Refresh();

            return directoryInfo;
        }

        public static DirectoryEntry EnsureExists(this DirectoryEntry directoryEntry)
        {
            if (directoryEntry == null)
            {
                throw new ArgumentNullException(nameof(directoryEntry));
            }

            try
            {
                if (!directoryEntry.FileSystem.DirectoryExists(directoryEntry.Path))
                {
                    directoryEntry.Create();
                }
            }
            catch (PathTooLongException ex)
            {
                throw new PathTooLongException(
                    $"Could not create directory '{directoryEntry.FullName}', path length {directoryEntry.FullName.Length}",
                    ex);
            }

            return new DirectoryEntry(directoryEntry.FileSystem, directoryEntry.Path);
        }

        public static void DeleteIfExists(this DirectoryInfo? directoryInfo, bool recursive = true)
        {
            if (directoryInfo is null)
            {
                return;
            }

            try
            {
                directoryInfo.Refresh();

                if (directoryInfo.Exists)
                {
                    FileInfo[] fileInfos;

                    try
                    {
                        fileInfos = directoryInfo.GetFiles();
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsFatal())
                        {
                            throw;
                        }

                        throw new IOException(
                            $"Could not get files for directory '{directoryInfo.FullName}' for deletion",
                            ex);
                    }

                    foreach (FileInfo file in fileInfos)
                    {
                        file.Attributes = FileAttributes.Normal;

                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            if (ex.IsFatal())
                            {
                                throw;
                            }

                            throw new IOException($"Could not delete file '{file.FullName}'", ex);
                        }
                    }

                    foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
                    {
                        subDirectory.DeleteIfExists(recursive);
                    }
                }

                directoryInfo.Refresh();

                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(recursive);
                }

                directoryInfo.Refresh();
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Could not delete directory '{directoryInfo.FullName}'", ex);
            }
        }
        public static void DeleteIfExists(this DirectoryEntry? directoryInfo, bool recursive = true)
        {
            if (directoryInfo is null)
            {
                return;
            }

            try
            {
                if (directoryInfo.Exists)
                {
                    FileEntry[] fileInfos;

                    try
                    {
                        fileInfos = directoryInfo.EnumerateFiles().ToArray();
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsFatal())
                        {
                            throw;
                        }

                        throw new IOException(
                            $"Could not get files for directory '{directoryInfo.FullName}' for deletion",
                            ex);
                    }

                    foreach (var file in fileInfos)
                    {
                        file.Attributes = FileAttributes.Normal;

                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            if (ex.IsFatal())
                            {
                                throw;
                            }

                            throw new IOException($"Could not delete file '{file.FullName}'", ex);
                        }
                    }

                    foreach (var subDirectory in directoryInfo.EnumerateDirectories())
                    {
                        subDirectory.DeleteIfExists(recursive);
                    }
                }

                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(recursive);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Could not delete directory '{directoryInfo.FullName}'", ex);
            }
        }

        public static ImmutableArray<FileEntry> GetFilesRecursive(
            this DirectoryEntry directoryInfo,
            IEnumerable<string>? fileExtensions = null,
            PathLookupSpecification? pathLookupSpecification = null,
            string? rootDir = null)
        {
            if (directoryInfo == null)
            {
                throw new ArgumentNullException(nameof(directoryInfo));
            }

            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"The directory '{directoryInfo.FullName}' does not exist");
            }

            PathLookupSpecification usedPathLookupSpecification =
                pathLookupSpecification ?? DefaultPaths.DefaultPathLookupSpecification;

            var usedFileExtensions = fileExtensions.SafeToReadOnlyCollection();

            if (usedPathLookupSpecification.IsNotAllowed(directoryInfo.FullName, rootDir).Item1)
            {
                return ImmutableArray<FileEntry>.Empty;
            }

            IReadOnlyCollection<string> invalidFileExtensions = usedFileExtensions
                .Where(fileExtension => !fileExtension.StartsWith(".", StringComparison.OrdinalIgnoreCase))
                .ToReadOnlyCollection();

            if (invalidFileExtensions.Count > 0)
            {
                throw new ArgumentException(Resources.FileExtensionMustStartWithDot);
            }

            var files = new List<FileEntry>();

            List<FileEntry> directoryFiles = directoryInfo
                .EnumerateFiles()
                .Where(file => !usedPathLookupSpecification.IsFileExcluded(file.FullName, rootDir).Item1)
                .ToList();

            List<FileEntry> filtered = (usedFileExtensions.Any()
                    ? directoryFiles.Where(file => usedFileExtensions.Any(extension => file.Path.GetExtensionWithDot().Equals(
                        extension,
                        StringComparison.OrdinalIgnoreCase)))
                    : directoryFiles)
                .ToList();

            files.AddRange(filtered);

            DirectoryEntry[] subDirectories = directoryInfo.EnumerateDirectories().ToArray();

            files.AddRange(subDirectories
                .SelectMany(dir => dir.GetFilesRecursive(usedFileExtensions, usedPathLookupSpecification, rootDir)));

            return files.ToImmutableArray();
        }

        public static ImmutableArray<FileEntry> GetFilesWithWithExclusions(
            [NotNull] this DirectoryEntry siteArtifactDirectory,
            IFileSystem fileSystem,
            [NotNull] IReadOnlyCollection<string> excludedPatterns)
        {
            if (siteArtifactDirectory == null)
            {
                throw new ArgumentNullException(nameof(siteArtifactDirectory));
            }

            if (excludedPatterns == null)
            {
                throw new ArgumentNullException(nameof(excludedPatterns));
            }


            var allFiles = fileSystem
                .EnumerateFileEntries(siteArtifactDirectory.Path, "*", SearchOption.AllDirectories)
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
                        if (excludedPattern.Contains(Path.DirectorySeparatorChar, StringComparison.InvariantCulture))
                        {
                            excludedFiles = allFiles.Where(file =>
                                    file.FullName.Substring(siteArtifactDirectory.FullName.Length)
                                        .Contains(excludedPattern, StringComparison.OrdinalIgnoreCase))
                                .ToArray();
                        }
                        else
                        {
                            excludedFiles =
                                fileSystem.EnumerateFileEntries(siteArtifactDirectory.Path,excludedPattern, SearchOption.AllDirectories)
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
}