using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.Defensive.Collections;
using Arbor.Exceptions;

namespace Arbor.X.Core.IO
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

        public static IReadOnlyCollection<FileInfo> GetFilesRecursive(
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
            IEnumerable<string> usedFileExtensions = fileExtensions ?? new List<string>();

            if (usedPathLookupSpecification.IsBlackListed(directoryInfo.FullName, rootDir).Item1)
            {
                return new List<FileInfo>();
            }

            IReadOnlyCollection<string> invalidFileExtensions = usedFileExtensions
                .Where(fileExtension => !fileExtension.StartsWith(".", StringComparison.OrdinalIgnoreCase))
                .ToReadOnlyCollection();

            if (invalidFileExtensions.Any())
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
                .SelectMany(dir => dir.GetFilesRecursive(fileExtensions, usedPathLookupSpecification, rootDir))
                .ToList());

            return files;
        }
    }
}
