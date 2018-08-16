using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Packaging;
using Serilog;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class ManitestReWriter
    {
        public ManifestReWriteResult Rewrite(
            string nuspecFullPath,
            string tagPrefix = "x-arbor-x",
            ILogger logger = null)
        {
            if (string.IsNullOrWhiteSpace(nuspecFullPath))
            {
                throw new ArgumentNullException(nameof(nuspecFullPath));
            }

            if (string.IsNullOrWhiteSpace(tagPrefix))
            {
                throw new ArgumentNullException(nameof(tagPrefix));
            }

            if (!File.Exists(nuspecFullPath))
            {
                throw new FileNotFoundException($"The file '{nuspecFullPath}' does not exist", nuspecFullPath);
            }

            var removeTags = new List<string>();

            bool isReWritten = false;

            string tempFile = $"{nuspecFullPath}.{Guid.NewGuid()}.tmp";

            DirectoryInfo baseDir = new FileInfo(nuspecFullPath).Directory;

            using (var stream = new FileStream(nuspecFullPath, FileMode.Open))
            {
                var packageBuilder = new PackageBuilder(stream, baseDir.FullName);

                logger?.Verbose("Using starts with-pattern '{TagPrefix}' to exclude tags from NuSpec", tagPrefix);

                string[] tagsToRemove = packageBuilder.Tags
                    .Where(tag => tag.StartsWith(tagPrefix, StringComparison.Ordinal)).ToArray();

                if (tagsToRemove.Length == 0)
                {
                    logger?.Verbose("No tags to remove from NuSpec '{NuspecFullPath}'", nuspecFullPath);
                }

                foreach (string tagToRemove in tagsToRemove)
                {
                    logger?.Verbose("Removing tag '{TagToRemove}' from NuSpec '{NuspecFullPath}'",
                        tagToRemove,
                        nuspecFullPath);
                    packageBuilder.Tags.Remove(tagToRemove);
                }

                if (tagsToRemove.Length > 0)
                {
                    using (var outStream = new FileStream(tempFile, FileMode.CreateNew))
                    {
                        packageBuilder.Save(outStream);
                        isReWritten = true;
                    }
                }
            }

            if (isReWritten)
            {
                logger?.Verbose("Deleting NuSpec file '{NuspecFullPath}'", nuspecFullPath);
                File.Delete(nuspecFullPath);

                logger?.Verbose("Moving NuSpec temp copy '{TempFile}' to file '{NuspecFullPath}'",
                    tempFile,
                    nuspecFullPath);
                File.Move(tempFile, nuspecFullPath);
            }

            var result = new ManifestReWriteResult(removeTags, tagPrefix);

            return result;
        }
    }
}
