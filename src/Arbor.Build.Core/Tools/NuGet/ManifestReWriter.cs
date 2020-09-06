using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Packaging;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class ManifestReWriter
    {
        private readonly IFileSystem _fileSystem;

        public ManifestReWriter(IFileSystem fileSystem) => _fileSystem = fileSystem;

        public ManifestReWriteResult Rewrite(
            string nuspecFullPath,
            Func<string, string?>? propertyProvider = null,
            string tagPrefix = "x-arbor-build",
            ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(nuspecFullPath))
            {
                throw new ArgumentNullException(nameof(nuspecFullPath));
            }

            if (string.IsNullOrWhiteSpace(tagPrefix))
            {
                throw new ArgumentNullException(nameof(tagPrefix));
            }

            var nuspecFileSystemFullPath = _fileSystem.ConvertPathFromInternal(nuspecFullPath);

            if (!_fileSystem.FileExists(nuspecFileSystemFullPath))
            {
                throw new FileNotFoundException($"The file '{nuspecFileSystemFullPath}' does not exist",
                    nuspecFileSystemFullPath.FullName);
            }

            bool isReWritten = false;

            string? tempFile = null;

            var removeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            PackageBuilder? packageBuilder;

            using (var nuspecReadStream =
                _fileSystem.OpenFile(nuspecFileSystemFullPath, FileMode.Open, FileAccess.Read))
            {
                DirectoryEntry baseDir = new FileEntry(_fileSystem, nuspecFileSystemFullPath).Directory;
                string? baseDirFileSystemPath = _fileSystem.ConvertPathToInternal(baseDir.Path);

                packageBuilder = new PackageBuilder(nuspecReadStream, baseDirFileSystemPath, propertyProvider);
            }

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

                if (removeTags.Count == 0)
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

                    using var nuspecReadStream =
                        _fileSystem.OpenFile(nuspecFileSystemFullPath, FileMode.Open, FileAccess.Read);
                    var manifest = Manifest.ReadFrom(nuspecReadStream, propertyProvider, true);

                    manifest.Metadata.Tags = string.Join(" ", packageBuilder.Tags);

                    using var tempStream = _fileSystem.OpenFile(tempFile, FileMode.Create, FileAccess.Write);
                    manifest.Save(tempStream);
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