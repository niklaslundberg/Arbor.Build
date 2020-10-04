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
        public static DirectoryEntry EnsureExists(this DirectoryEntry directoryEntry)
        {
            if (directoryEntry == null)
            {
                throw new ArgumentNullException(nameof(directoryEntry));
            }

            try
            {

                directoryEntry.Create();

            }
            catch (System.IO.IOException ex) when (ex.Message.Contains("already exists", StringComparison.InvariantCultureIgnoreCase))
            {
                return directoryEntry;
            }
            catch (PathTooLongException ex)
            {
                throw new PathTooLongException(
                    $"Could not create directory '{directoryEntry.FullName}', path length {directoryEntry.FullName.Length}",
                    ex);
            }

            return new DirectoryEntry(directoryEntry.FileSystem, directoryEntry.Path);
        }

        public static void DeleteIfExists(this DirectoryEntry? directoryEntry, bool recursive = true)
        {
            if (directoryEntry is null)
            {
                return;
            }

            try
            {
                if (directoryEntry.Exists)
                {
                    FileEntry[] files;

                    try
                    {
                        files = directoryEntry.EnumerateFiles().ToArray();
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsFatal())
                        {
                            throw;
                        }

                        throw new IOException(
                            $"Could not get files for directory '{directoryEntry.FullName}' for deletion",
                            ex);
                    }

                    foreach (var file in files)
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

                    foreach (var subDirectory in directoryEntry.EnumerateDirectories())
                    {
                        subDirectory.DeleteIfExists(recursive);
                    }
                }

                if (directoryEntry.Exists)
                {
                    directoryEntry.Delete(recursive);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException($"Could not delete directory '{directoryEntry.FullName}'", ex);
            }
        }

        public static bool DeleteIfExists(this FileEntry? file)
        {
            if (file is null)
            {
                return false;
            }

            try
            {
                file.Delete();
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException($"Unauthorized to delete file '{file.FullName}'", ex);
            }
        }

        public static ImmutableArray<FileEntry> GetFilesRecursive(
            this DirectoryEntry directory,
            IEnumerable<string>? fileExtensions = null,
            PathLookupSpecification? pathLookupSpecification = null,
            DirectoryEntry? rootDir = null)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (!directory.Exists)
            {
                throw new DirectoryNotFoundException($"The directory '{directory.FullName}' does not exist");
            }

            PathLookupSpecification usedPathLookupSpecification =
                pathLookupSpecification ?? DefaultPaths.DefaultPathLookupSpecification;

            var usedFileExtensions = fileExtensions.SafeToReadOnlyCollection();

            if (usedPathLookupSpecification.IsNotAllowed(directory, rootDir).Item1)
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

            var directoryFiles = directory
                .EnumerateFiles()
                .Where(file => !usedPathLookupSpecification.IsFileExcluded(file, rootDir).Item1)
                .ToList();

            var filtered = (usedFileExtensions.Any()
                    ? directoryFiles.Where(file => usedFileExtensions.Any(extension => file.Path.GetExtensionWithDot().Equals(
                        extension,
                        StringComparison.OrdinalIgnoreCase)))
                    : directoryFiles)
                .ToList();

            files.AddRange(filtered);

            DirectoryEntry[] subDirectories = directory.EnumerateDirectories().ToArray();

            files.AddRange(subDirectories
                .SelectMany(dir => dir.GetFilesRecursive(usedFileExtensions, usedPathLookupSpecification, rootDir)));

            return files.ToImmutableArray();
        }

        public static ImmutableArray<FileEntry> GetFilesWithWithExclusions(
            [NotNull] this DirectoryEntry directory,
            [NotNull] IReadOnlyCollection<string> excludedPatterns)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (excludedPatterns == null)
            {
                throw new ArgumentNullException(nameof(excludedPatterns));
            }

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
                        if (excludedPattern.Contains(UPath.DirectorySeparator, StringComparison.InvariantCulture))
                        {
                            excludedFiles = allFiles.Where(file =>
                                    file.FullName.Substring(directory.FullName.Length)
                                        .Contains(excludedPattern, StringComparison.OrdinalIgnoreCase))
                                .ToArray();
                        }
                        else
                        {
                            excludedFiles =
                                fileSystem.EnumerateFileEntries(directory.Path,excludedPattern, SearchOption.AllDirectories)
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