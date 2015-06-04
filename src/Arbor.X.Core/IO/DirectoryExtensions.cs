using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.X.Core.Tools;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;

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
                    string.Format("Could not create directory '{0}', path length {1}", directoryInfo.FullName,
                        directoryInfo.FullName.Length), ex);
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
                        foreach (var file in directoryInfo.EnumerateFiles())
                        {
                            file.Attributes = FileAttributes.Normal;
                            file.Delete();
                        }

                        foreach (var subDirectory in directoryInfo.EnumerateDirectories())
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
                throw new InvalidOperationException(string.Format("Could not delete directory '{0}'", directoryInfo.FullName), ex);
            }
        }

        public static IReadOnlyCollection<FileInfo> GetFilesRecursive(this DirectoryInfo directoryInfo,
            IEnumerable<string> fileExtensions = null, PathLookupSpecification pathLookupSpecification = null, string rootDir = null)
        {

            if (directoryInfo == null)
            {
                throw new ArgumentNullException(nameof(directoryInfo));
            }

            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"The directory '{directoryInfo.FullName}' does not exist");
            }

            PathLookupSpecification usedPathLookupSpecification = pathLookupSpecification ?? DefaultPaths.DefaultPathLookupSpecification;
            var usedFileExtensions = fileExtensions ?? new List<string>();

            if (usedPathLookupSpecification.IsBlackListed(directoryInfo.FullName, rootDir))
            {
                return new List<FileInfo>();
            }

            var invalidFileExtensions = usedFileExtensions
                .Where(fileExtension => !fileExtension.StartsWith("."))
                .ToReadOnlyCollection();

            if (invalidFileExtensions.Any())
            {
                throw new ArgumentException("File extensions must start with '.', eg .txt");
            }

            List<FileInfo> files = new List<FileInfo>();

            var directoryFiles = directoryInfo
                .GetFiles()
                .Where(file => !usedPathLookupSpecification.IsFileBlackListed(file.FullName, rootDir))
                .ToList();

            var filtered = (usedFileExtensions.Any()
                ? directoryFiles.Where(file => usedFileExtensions.Any(extension => file.Extension.Equals(extension, StringComparison.InvariantCultureIgnoreCase)))
                : directoryFiles)
                .ToList();

            files.AddRange(filtered);

            var subDirectories = directoryInfo.GetDirectories();

            files.AddRange(subDirectories.SelectMany(dir => dir.GetFilesRecursive(fileExtensions, usedPathLookupSpecification, rootDir)).ToList());

            return files;
        }
    }
}