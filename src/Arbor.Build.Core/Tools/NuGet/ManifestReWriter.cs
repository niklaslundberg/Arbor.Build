using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arbor.FS;
using NuGet.Packaging;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

public class ManifestReWriter(IFileSystem fileSystem)
{
    private static readonly char[] Separator = [' '];

    private static ISet<string> ParseTags(string tags) =>
        tags.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<ManifestReWriteResult> Rewrite(
        FileEntry nuspecFullPath,
        Func<string, string?>? propertyProvider = null,
        string tagPrefix = "x-arbor-build",
        ILogger? logger = null)
    {
        bool isReWritten = false;

        FileEntry? tempFile = null;

        var removeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Manifest manifest;

        await using (var nuspecReadStream = nuspecFullPath.Open(FileMode.Open, FileAccess.Read))
        {
            manifest = Manifest.ReadFrom(nuspecReadStream, propertyProvider, true);
        }

        ISet<string> tags = ParseTags(manifest.Metadata.Tags);

        if (tags.Count > 0)
        {
            tempFile = new FileEntry(fileSystem,
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

                manifest.Metadata.Tags = string.Join(" ", tags);

                await using var tempStream = tempFile.Open(FileMode.Create, FileAccess.Write);
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

        return new ManifestReWriteResult(removeTags, tagPrefix, tempFile is { Exists: true } ? tempFile : null);
    }
}