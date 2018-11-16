using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Arbor.Defensive.Collections;
using Arbor.Exceptions;
using JetBrains.Annotations;

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

        public static void DeleteIfExists(this DirectoryInfo directoryInfo, bool recursive = true)
        {
            try
            {
                if (directoryInfo != null)
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
            }
            catch (UnauthorizedAccessException ex)
            {
                if (directoryInfo != null)
                {
                    throw new InvalidOperationException($"Could not delete directory '{directoryInfo.FullName}'", ex);
                }

                throw;
            }
        }

        public static ImmutableArray<FileInfo> GetFilesRecursive(
            this DirectoryInfo directoryInfo,
            IEnumerable<string> fileExtensions = null,
            PathLookupSpecification pathLookupSpecification = null,
            string rootDir = null)
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

            ImmutableArray<string> usedFileExtensions = fileExtensions.SafeToReadOnlyCollection();

            if (usedPathLookupSpecification.IsBlackListed(directoryInfo.FullName, rootDir).Item1)
            {
                return ImmutableArray<FileInfo>.Empty;
            }

            IReadOnlyCollection<string> invalidFileExtensions = usedFileExtensions
                .Where(fileExtension => !fileExtension.StartsWith(".", StringComparison.OrdinalIgnoreCase))
                .ToReadOnlyCollection();

            if (invalidFileExtensions.Count > 0)
            {
                throw new ArgumentException("File extensions must start with '.', eg .txt");
            }

            var files = new List<FileInfo>();

            List<FileInfo> directoryFiles = directoryInfo
                .GetFiles()
                .Where(file => !usedPathLookupSpecification.IsFileBlackListed(file.FullName, rootDir).Item1)
                .ToList();

            List<FileInfo> filtered = (usedFileExtensions.Any()
                    ? directoryFiles.Where(file => usedFileExtensions.Any(extension => file.Extension.Equals(
                        extension,
                        StringComparison.InvariantCultureIgnoreCase)))
                    : directoryFiles)
                .ToList();

            files.AddRange(filtered);

            DirectoryInfo[] subDirectories = directoryInfo.GetDirectories();

            files.AddRange(subDirectories
                .SelectMany(dir => dir.GetFilesRecursive(usedFileExtensions, usedPathLookupSpecification, rootDir)));

            return files.ToImmutableArray();
        }

        public static ImmutableArray<string> GetFilesWithWithExclusions(
            [NotNull] this DirectoryInfo siteArtifactDirectory,
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

            string[] allFiles = siteArtifactDirectory.GetFiles("*", SearchOption.AllDirectories).Select(s => s.FullName)
                .ToArray();

            string[] allIncludedFiles;

            if (excludedPatterns.Count == 0)
            {
                allIncludedFiles = allFiles;
            }
            else
            {
                var excludedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string excludedPattern in excludedPatterns.Where(p => p.Length > 0))
                {
                    string[] excludedFiles;
                    siteArtifactDirectory.Refresh();
                    try
                    {
                        if (excludedPattern.IndexOf(Path.DirectorySeparatorChar, StringComparison.InvariantCulture) >=
                            0)
                        {
                            excludedFiles = allFiles.Where(file =>
                                    file.Substring(siteArtifactDirectory.FullName.Length)
                                        .IndexOf(excludedPattern, StringComparison.OrdinalIgnoreCase) >= 0)
                                .ToArray();
                        }
                        else
                        {
                            excludedFiles =
                                siteArtifactDirectory.GetFiles(excludedPattern, SearchOption.AllDirectories)
                                    .Select(file => file.FullName).ToArray();
                        }
                    }
                    catch (Exception ex) when (!ex.IsFatal())
                    {
                        throw new InvalidOperationException($"Could not get files with pattern '{excludedPattern}'");
                    }

                    foreach (string excludedFile in excludedFiles)
                    {
                        excludedFileNames.Add(excludedFile);
                    }
                }

                allIncludedFiles = allFiles
                    .Except(excludedFileNames, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (string file in allIncludedFiles)
                {
                    if (!File.Exists(file))
                    {
                        throw new InvalidOperationException($"The file '{file}' does not exist");
                    }
                }
            }

            return allIncludedFiles.ToImmutableArray();
        }
    }
}
