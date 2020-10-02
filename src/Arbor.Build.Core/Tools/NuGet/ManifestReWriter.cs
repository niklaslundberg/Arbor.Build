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
            UPath nuspecFullPath,
            Func<string, string?>? propertyProvider = null,
            string tagPrefix = "x-arbor-build",
            ILogger? logger = null)
        {
            if (nuspecFullPath.FullName is null)
            {
                throw new ArgumentNullException(nameof(nuspecFullPath));
            }

            if (string.IsNullOrWhiteSpace(tagPrefix))
            {
                throw new ArgumentNullException(nameof(tagPrefix));
            }


            if (!_fileSystem.FileExists(nuspecFullPath))
            {
                throw new FileNotFoundException($"The file '{nuspecFullPath}' does not exist",
                    nuspecFullPath.FullName);
            }

            bool isReWritten = false;

            FileEntry? tempFile = null;

            var removeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DirectoryEntry baseDir = new FileEntry(_fileSystem, nuspecFullPath).Directory;
            string? baseDirFileSystemPath = _fileSystem.ConvertPathToInternal(baseDir.Path);

            ISet<string> tags;

            using (var nuspecReadStream = _fileSystem.OpenFile(nuspecFullPath, FileMode.Open, FileAccess.Read))
            {
                var packageBuilder = new PackageBuilder(nuspecReadStream, baseDirFileSystemPath, propertyProvider);
                tags = packageBuilder.Tags;
            }

            if (tags.Count > 0)
            {
                tempFile = new FileEntry(_fileSystem,
                    UPath.Combine(nuspecFullPath.GetDirectory(),
                        $"{nuspecFullPath.GetNameWithoutExtension()}.rewrite.nuspec"));

                logger?.Verbose("Using starts with-pattern '{TagPrefix}' to exclude tags from NuSpec", tagPrefix);

                string[] matchingTags = tags
                    .Where(tag => tag.StartsWith(tagPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (tags.Any(tag =>
                    tag.Equals(WellKnownNuGetTags.NoSource, StringComparison.OrdinalIgnoreCase)))
                {
                    removeTags.Add(WellKnownNuGetTags.NoSource);
                }

                if (removeTags.Count == 0)
                {
                    logger?.Verbose("No tags to remove from NuSpec '{NuspecFullPath}'", nuspecFullPath);
                }
                else
                {
                    removeTags.AddRange(matchingTags);
                    foreach (string tagToRemove in removeTags)
                    {
                        logger?.Verbose("Removing tag '{TagToRemove}' from NuSpec '{NuspecFullPath}'",
                            tagToRemove,
                            nuspecFullPath);
                        tags.Remove(tagToRemove);
                    }

                    using (var nuspecReadStream =
                        _fileSystem.OpenFile(nuspecFullPath, FileMode.Open, FileAccess.Read))
                    {
                        var manifest = Manifest.ReadFrom(nuspecReadStream, propertyProvider, true);

                        manifest.Metadata.Tags = string.Join(" ", tags);

                        using (var tempStream = tempFile.Open(FileMode.Create, FileAccess.Write))
                        {
                            manifest.Save(tempStream);
                        }
                    }

                    isReWritten = true;
                }
            }

            if (isReWritten && tempFile is {})
            {
                logger?.Verbose("Deleting NuSpec file '{NuspecFullPath}'", nuspecFullPath);
                _fileSystem.DeleteFile(nuspecFullPath);

                logger?.Verbose("Moving NuSpec temp copy '{TempFile}' to file '{NuspecFullPath}'",
                    tempFile,
                    nuspecFullPath);
            }

            var result = new ManifestReWriteResult(removeTags, tagPrefix, tempFile is {} && tempFile.Exists ? tempFile : null);

            return result;
        }
    }
}