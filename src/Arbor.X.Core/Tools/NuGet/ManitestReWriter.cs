using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet;
using NuGet.Packaging;

using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.NuGet
{
    public class ManitestReWriter
    {
        public ManifestReWriteResult Rewrite(string nuspecFullPath, string tagPrefix = "x-arbor-x",
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

            List<string> removeTags = new List<string>();

            bool isReWritten = false;

            var tempFile = $"{nuspecFullPath}.{Guid.NewGuid()}.tmp";

            var baseDir = new FileInfo(nuspecFullPath).Directory;

            using (var stream = new FileStream(nuspecFullPath, FileMode.Open))
            {
                PackageBuilder packageBuilder = new PackageBuilder(stream, baseDir.FullName);

                logger?.WriteVerbose("Using starts with-pattern '" + tagPrefix + "' to exclude tags from NuSpec");

                var tagsToRemove = packageBuilder.Tags.Where(tag => tag.StartsWith(tagPrefix)).ToArray();

                if (!tagsToRemove.Any())
                {
                    logger?.WriteVerbose($"No tags to remove from NuSpec '{nuspecFullPath}'");
                }

                foreach (var tagToRemove in tagsToRemove)
                {
                    logger?.WriteVerbose($"Removing tag '{tagToRemove}' from NuSpec '{nuspecFullPath}'");
                    packageBuilder.Tags.Remove(tagToRemove);
                }

                if (tagsToRemove.Any())
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
                logger?.WriteVerbose($"Deleting NuSpec file '{nuspecFullPath}'");
                File.Delete(nuspecFullPath);

                logger?.WriteVerbose($"Moving NuSpec temp copy '{tempFile}' to file '{nuspecFullPath}'");
                File.Move(tempFile, nuspecFullPath);
            }

            var result = new ManifestReWriteResult(removeTags, tagPrefix);

            return result;
        }
    }
}