using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Defensive.Collections;
using Arbor.FS;
using Serilog;
using Zio;

namespace Arbor.Build.Core.IO
{
    public static class PathExtensions
    {
        public static (bool, string) IsFileExcluded(
            this PathLookupSpecification pathLookupSpecification,
            FileEntry sourceFile,
            DirectoryEntry? rootDir = null,
            bool allowNonExistingFiles = false,
            ILogger? logger = null)
        {
            if (pathLookupSpecification == null)
            {
                throw new ArgumentNullException(nameof(pathLookupSpecification));
            }

            if (sourceFile is null)
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            string internalPath = sourceFile.ConvertPathToInternal();

            if (!allowNonExistingFiles && !sourceFile.Exists)
            {
                string messageMessage = $"File '{internalPath}' does not exist";
                logger?.Debug("{Reason}", messageMessage);
                return (true, messageMessage);
            }

            (bool, string) directoryExcludeListed =
                pathLookupSpecification.IsNotAllowed(sourceFile.Directory, rootDir);

            if (directoryExcludeListed.Item1)
            {
                string reasonMessage = $"Directory of '{internalPath}' is not allowed, {directoryExcludeListed.Item2}";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            bool isNotAllowed = HasAnyPathSegmentStartsWith(
                sourceFile.Name,
                pathLookupSpecification.IgnoredFileStartsWithPatterns);

            if (isNotAllowed)
            {
                string reasonMessage = $"Path segments of '{internalPath}' makes it not allowed";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            IReadOnlyCollection<string> ignoredFileNameParts = pathLookupSpecification.IgnoredFileNameParts
                .Where(part => !string.IsNullOrEmpty(part))
                .Where(
                    part => sourceFile.Name.Contains(part, StringComparison.OrdinalIgnoreCase))
                .SafeToReadOnlyCollection();

            if (ignoredFileNameParts.Count > 0)
            {
                string reasonMessage =
                    $"Ignored file name parts of '{internalPath}' makes it not allowed: {string.Join(", ", ignoredFileNameParts.Select(item => $"'{item}'"))}";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            return (false, string.Empty);
        }

        public static (bool, string) IsNotAllowed(
            this PathLookupSpecification pathLookupSpecification,
            DirectoryEntry sourceDir,
            DirectoryEntry? rootDir = null,
            ILogger? logger = null)
        {
            if (pathLookupSpecification == null)
            {
                throw new ArgumentNullException(nameof(pathLookupSpecification));
            }

            if (sourceDir is null)
            {
                throw new ArgumentNullException(nameof(sourceDir));
            }

            string sourceInternalPath = sourceDir.ConvertPathToInternal();
            if (!sourceDir.Exists)
            {
                return (true, $"Source directory '{sourceInternalPath}' does not exist");
            }

            string[] sourceDirSegments = GetSourceDirSegments(sourceDir, rootDir);

            bool hasAnyPathSegment = HasAnyPathSegment(
                sourceDirSegments,
                pathLookupSpecification.IgnoredDirectorySegments,
                logger);

            if (hasAnyPathSegment)
            {
                string reasonMessage = $"The directory '{sourceInternalPath}' has a path segment that is not allowed";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            bool hasAnyPathSegmentPart = HasAnyPathSegmentPart(
                sourceDirSegments,
                pathLookupSpecification.IgnoredDirectorySegmentParts);

            if (hasAnyPathSegmentPart)
            {
                string reasonMessage = $"The directory '{sourceInternalPath}' has a path segment part that is not allowed";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            bool hasAnyPartStartsWith = HasAnyPathSegmentStartsWith(
                sourceDirSegments,
                pathLookupSpecification.IgnoredDirectoryStartsWithPatterns);

            if (hasAnyPartStartsWith)
            {
                string reasonMessage =
                    $"The directory '{sourceInternalPath}' has a path that starts with a pattern that is not allowed";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            logger?.Debug("The directory '{SourceDir}' is not not allowed", sourceInternalPath);

            return (false, string.Empty);
        }

        private static string[] GetSourceDirSegments(DirectoryEntry sourceDir, DirectoryEntry? rootDir)
        {
            string path = rootDir is null ? sourceDir.FullName : sourceDir.FullName.Replace(rootDir.FullName, string.Empty, StringComparison.OrdinalIgnoreCase);

            return path.Split(
                new[] { UPath.DirectorySeparator },
                StringSplitOptions.RemoveEmptyEntries);
        }

        public static UPath ParseAsPath(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
            }

            if (!UPath.TryParse(path, out var parsed))
            {
                throw new FormatException($"Could not parse '{path}' as a full path");
            }

            var normalized = parsed.NormalizePath();

            if (!normalized.IsAbsolute)
            {
                throw new FormatException($"Path {parsed.FullName} is not a full path");
            }

            return normalized;
        }

        private static bool HasAnyPathSegment(
            IEnumerable<string> segments,
            IEnumerable<string> patterns,
            ILogger? logger = null) => segments.Any(segment => HasAnyPathSegment(segment, patterns, logger));

        private static bool HasAnyPathSegment(string segment, IEnumerable<string> patterns, ILogger? logger = null) => patterns.Any(pattern =>
                                                                                                                                {
                                                                                                                                    bool isMatch = segment.Equals(pattern, StringComparison.OrdinalIgnoreCase);

                                                                                                                                    if (isMatch)
                                                                                                                                    {
                                                                                                                                        logger?.Debug("Segment '{Segment}' matches pattern '{Pattern}'", segment, pattern);
                                                                                                                                    }

                                                                                                                                    return isMatch;
                                                                                                                                });

        private static bool HasAnyPathSegmentStartsWith(IEnumerable<string> segments, IEnumerable<string> patterns) => segments.Any(segment => HasAnyPathSegmentStartsWith(segment, patterns));

        private static bool HasAnyPathSegmentStartsWith(string segment, IEnumerable<string> patterns) => patterns.Any(pattern => segment.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));

        private static bool HasAnyPathSegmentPart(IEnumerable<string> segments, IEnumerable<string> patterns) => segments.Any(segment => HasAnyPathSegmentPart(segment, patterns));

        private static bool HasAnyPathSegmentPart(string segment, IEnumerable<string> patterns) => patterns.Any(pattern => segment.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
