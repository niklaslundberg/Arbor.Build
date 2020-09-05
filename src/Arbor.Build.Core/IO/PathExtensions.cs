using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.Defensive.Collections;
using Serilog;

namespace Arbor.Build.Core.IO
{
    public static class PathExtensions
    {
        public static (bool, string) IsFileExcluded(
            this PathLookupSpecification pathLookupSpecification,
            string sourceFile,
            string? rootDir = null,
            bool allowNonExistingFiles = false,
            ILogger? logger = null)
        {
            if (pathLookupSpecification == null)
            {
                throw new ArgumentNullException(nameof(pathLookupSpecification));
            }

            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            if (!allowNonExistingFiles && !File.Exists(sourceFile))
            {
                string messageMessage = $"File '{sourceFile}' does not exist";
                logger?.Debug("{Reason}", messageMessage);
                return (true, messageMessage);
            }

            var sourceFileInfo = new FileInfo(sourceFile);

            (bool, string) directoryExcludeListed =
                pathLookupSpecification.IsNotAllowed(sourceFileInfo.Directory?.FullName, rootDir);

            if (directoryExcludeListed.Item1)
            {
                string reasonMessage = $"Directory of '{sourceFile}' is not allowed, {directoryExcludeListed.Item2}";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            bool isNotAllowed = HasAnyPathSegmentStartsWith(
                sourceFileInfo.Name,
                pathLookupSpecification.IgnoredFileStartsWithPatterns);

            if (isNotAllowed)
            {
                string reasonMessage = $"Path segments of '{sourceFile}' makes it not allowed";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            IReadOnlyCollection<string> ignoredFileNameParts = pathLookupSpecification.IgnoredFileNameParts
                .Where(part => !string.IsNullOrEmpty(part))
                .Where(
                    part => sourceFileInfo.Name.Contains(part, StringComparison.OrdinalIgnoreCase))
                .SafeToReadOnlyCollection();

            if (ignoredFileNameParts.Count > 0)
            {
                string reasonMessage =
                    $"Ignored file name parts of '{sourceFile}' makes it not allowed: {string.Join(", ", ignoredFileNameParts.Select(item => $"'{item}'"))}";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            return (false, string.Empty);
        }

        public static (bool, string) IsNotAllowed(
            this PathLookupSpecification pathLookupSpecification,
            string sourceDir,
            string? rootDir = null,
            ILogger? logger = null)
        {
            if (pathLookupSpecification == null)
            {
                throw new ArgumentNullException(nameof(pathLookupSpecification));
            }

            if (string.IsNullOrWhiteSpace(sourceDir))
            {
                throw new ArgumentNullException(nameof(sourceDir));
            }

            if (!Directory.Exists(sourceDir))
            {
                return (true, $"Source directory '{sourceDir}' does not exist");
            }

            string[] sourceDirSegments = GetSourceDirSegments(sourceDir, rootDir);

            bool hasAnyPathSegment = HasAnyPathSegment(
                sourceDirSegments,
                pathLookupSpecification.IgnoredDirectorySegments,
                logger);

            if (hasAnyPathSegment)
            {
                string reasonMessage = $"The directory '{sourceDir}' has a path segment that is not allowed";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            bool hasAnyPathSegmentPart = HasAnyPathSegmentPart(
                sourceDirSegments,
                pathLookupSpecification.IgnoredDirectorySegmentParts);

            if (hasAnyPathSegmentPart)
            {
                string reasonMessage = $"The directory '{sourceDir}' has a path segment part that is not allowed";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            bool hasAnyPartStartsWith = HasAnyPathSegmentStartsWith(
                sourceDirSegments,
                pathLookupSpecification.IgnoredDirectoryStartsWithPatterns);

            if (hasAnyPartStartsWith)
            {
                string reasonMessage =
                    $"The directory '{sourceDir}' has a path that starts with a pattern that is not allowed";
                logger?.Debug("{Reason}", reasonMessage);
                return (true, reasonMessage);
            }

            logger?.Debug("The directory '{SourceDir}' is not not allowed", sourceDir);

            return (false, string.Empty);
        }

        private static string[] GetSourceDirSegments(string sourceDir, string rootDir)
        {
            string path = string.IsNullOrWhiteSpace(rootDir) ? sourceDir : sourceDir.Replace(rootDir, string.Empty);

            string[] sourceDirSegments = path.Split(
                new[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);
            return sourceDirSegments;
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
