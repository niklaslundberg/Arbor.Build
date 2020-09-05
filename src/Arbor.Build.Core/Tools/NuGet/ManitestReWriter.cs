using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Packaging;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class ManitestReWriter
    {
        private readonly IFileSystem _fileSystem;

        public ManitestReWriter(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public ManifestReWriteResult Rewrite(
            string nuspecFullPath2,
            string tagPrefix = "x-arbor-build",
            ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(nuspecFullPath2))
            {
                throw new ArgumentNullException(nameof(nuspecFullPath2));
            }

            if (string.IsNullOrWhiteSpace(tagPrefix))
            {
                throw new ArgumentNullException(nameof(tagPrefix));
            }

            var nuspecFileSystemFullPath = _fileSystem.ConvertPathFromInternal(nuspecFullPath2);

            if (!_fileSystem.FileExists(nuspecFileSystemFullPath))
            {
                throw new FileNotFoundException($"The file '{nuspecFileSystemFullPath}' does not exist", nuspecFileSystemFullPath.FullName);
            }
            using var stream = _fileSystem.OpenFile(nuspecFileSystemFullPath, FileMode.Open, FileAccess.Read);


            DirectoryEntry baseDir = new FileEntry(_fileSystem, nuspecFileSystemFullPath).Directory;
            var baseDirFileSystemPath = _fileSystem.ConvertPathToInternal(baseDir.Path);

            var packageBuilder = new PackageBuilder(stream, baseDirFileSystemPath);

            var removeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool isReWritten = false;

            string? tempFile = null;

            if (packageBuilder.Tags.Count > 0)
            {
                tempFile = $"{nuspecFileSystemFullPath}.{Guid.NewGuid()}.tmp";

                logger?.Verbose("Using starts with-pattern '{TagPrefix}' to exclude tags from NuSpec", tagPrefix);

                string[] matchingTags = packageBuilder.Tags
                    .Where(tag => tag.StartsWith(tagPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (packageBuilder.Tags.Any(tag =>
                    tag.Equals(WellKnownNuGetTags.NoSource, StringComparison.OrdinalIgnoreCase)))
                {
                    removeTags.Add(WellKnownNuGetTags.NoSource);
                }

                if (matchingTags.Length == 0)
                {
                    logger?.Verbose("No tags to remove from NuSpec '{NuspecFullPath}'", nuspecFileSystemFullPath);
                }
                else
                {
                    removeTags.AddRange(matchingTags);
                    foreach (string tagToRemove in removeTags)
                    {
                        logger?.Verbose("Removing tag '{TagToRemove}' from NuSpec '{NuspecFullPath}'",
                            tagToRemove,
                            nuspecFileSystemFullPath);
                        packageBuilder.Tags.Remove(tagToRemove);
                    }


                    using var outStream = _fileSystem.OpenFile(tempFile, FileMode.CreateNew, FileAccess.Write);
                    packageBuilder.Save(outStream);
                    isReWritten = true;
                }
            }

            if (isReWritten && tempFile is {})
            {
                logger?.Verbose("Deleting NuSpec file '{NuspecFullPath}'", nuspecFileSystemFullPath);
                _fileSystem.DeleteFile(nuspecFileSystemFullPath);

                logger?.Verbose("Moving NuSpec temp copy '{TempFile}' to file '{NuspecFullPath}'",
                    tempFile,
                    nuspecFileSystemFullPath);
                _fileSystem.MoveFile(tempFile, nuspecFileSystemFullPath);
            }

            var result = new ManifestReWriteResult(removeTags, tagPrefix);

            return result;
        }
    }
}
