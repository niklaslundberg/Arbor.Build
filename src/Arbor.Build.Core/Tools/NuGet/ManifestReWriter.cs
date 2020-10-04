﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arbor.Build.Core.IO;
using NuGet.Packaging;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class ManifestReWriter
    {
        private readonly IFileSystem _fileSystem;

        public ManifestReWriter(IFileSystem fileSystem) => _fileSystem = fileSystem;

        public async Task<ManifestReWriteResult> Rewrite(
            FileEntry nuspecFullPath,
            Func<string, string?>? propertyProvider = null,
            string tagPrefix = "x-arbor-build",
            ILogger? logger = null)
        {
            bool isReWritten = false;

            FileEntry? tempFile = null;

            var removeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DirectoryEntry baseDir = nuspecFullPath.Directory;
            string baseDirFileSystemPath = _fileSystem.ConvertPathToInternal(baseDir.Path);

            await using var memoryStream              = new MemoryStream();
            await using var packageBuilderStream              = new MemoryStream();

            await using (var nuspecReadStream = nuspecFullPath.Open(FileMode.Open, FileAccess.Read))
            {
                await nuspecReadStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
            }

            await memoryStream.CopyToAsync(packageBuilderStream);
            packageBuilderStream.Position = 0;

            var packageBuilder = new PackageBuilder(packageBuilderStream, baseDirFileSystemPath, propertyProvider);
            ISet<string> tags = packageBuilder.Tags;

            memoryStream.Position = 0;

            if (tags.Count > 0)
            {
                tempFile = new FileEntry(_fileSystem,
                    UPath.Combine(nuspecFullPath.Directory.Path,
                        $"{nuspecFullPath.Path.GetNameWithoutExtension()}.rewrite.nuspec"));

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

                    var manifest = Manifest.ReadFrom(memoryStream, propertyProvider, true);

                    manifest.Metadata.Tags = string.Join(" ", tags);

                    using var tempStream = tempFile.Open(FileMode.Create, FileAccess.Write);
                    manifest.Save(tempStream);

                    isReWritten = true;
                }
            }

            if (isReWritten && tempFile is {})
            {
                logger?.Verbose("Deleting NuSpec file '{NuspecFullPath}'", nuspecFullPath);
                nuspecFullPath.DeleteIfExists();

                logger?.Verbose("Moving NuSpec temp copy '{TempFile}' to file '{NuspecFullPath}'",
                    tempFile,
                    nuspecFullPath);
            }

            var result = new ManifestReWriteResult(removeTags, tagPrefix, tempFile is {} && tempFile.Exists ? tempFile : null);

            return result;
        }
    }
}