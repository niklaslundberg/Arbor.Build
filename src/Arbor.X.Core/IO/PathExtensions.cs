using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.Defensive.Collections;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.IO
{
    public static class PathExtensions
    {
        public static bool IsFileBlackListed(this PathLookupSpecification pathLookupSpecification, string sourceFile, string rootDir = null, bool allowNonExistingFiles = false, ILogger logger = null)
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
                logger?.WriteDebug($"File '{sourceFile}' does not exist");
                return true;
            }

            var sourceFileInfo = new FileInfo(sourceFile);

            if (pathLookupSpecification.IsBlackListed(sourceFileInfo.Directory.FullName, rootDir))
            {
                logger?.WriteDebug($"Directory of '{sourceFile}' is blacklisted");
                return true;
            }

            bool isBlackListed = HasAnyPathSegmentStartsWith(sourceFileInfo.Name, pathLookupSpecification.IgnoredFileStartsWithPatterns);

            if (isBlackListed)
            {
                logger?.WriteDebug($"Path segments of '{sourceFile}' makes it blacklisted");
            }

            var ignoredFileNameParts = pathLookupSpecification.IgnoredFileNameParts.Where(part => !string.IsNullOrEmpty(part)).Where(
                part => sourceFileInfo.Name.IndexOf(part, StringComparison.InvariantCultureIgnoreCase) >= 0).SafeToReadOnlyCollection();

            isBlackListed = isBlackListed || ignoredFileNameParts.Any();

            if (ignoredFileNameParts.Any())
            {
                logger?.WriteDebug($"Ignored file name parts of '{sourceFile}' makes it blacklisted: {string.Join(", ", ignoredFileNameParts.Select(item => $"'{item}'"))}");
            }

            return isBlackListed;
        }

        public static bool IsBlackListed(this PathLookupSpecification pathLookupSpecification, string sourceDir, string rootDir = null, ILogger logger = null)
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
                return true;
            }

            var sourceDirSegments = GetSourceDirSegments(sourceDir, rootDir);

            bool hasAnyPathSegment = HasAnyPathSegment(sourceDirSegments,
                pathLookupSpecification.IgnoredDirectorySegments, logger);

            if (hasAnyPathSegment)
            {
                logger?.WriteDebug($"The directory '{sourceDir}' has a path segment that is blacklisted");
                return true;
            }

            bool hasAnyPathSegmentPart = HasAnyPathSegmentPart(sourceDirSegments,
                pathLookupSpecification.IgnoredDirectorySegmentParts);

            if (hasAnyPathSegmentPart)
            {
                logger?.WriteDebug($"The directory '{sourceDir}' has a path segment part that is blacklisted");
                return true;
            }

            bool hasAnyPartStartsWith = HasAnyPathSegmentStartsWith(sourceDirSegments,
                pathLookupSpecification.IgnoredDirectoryStartsWithPatterns);

            if (hasAnyPartStartsWith)
            {
                logger?.WriteDebug($"The directory '{sourceDir}' has a path that starts with a pattern that is blacklisted");
                return true;
            }

            logger?.WriteDebug($"The directory '{sourceDir}' is not blacklisted");

            return false;
        }

        private static string[] GetSourceDirSegments(string sourceDir, string rootDir)
        {
            var path = string.IsNullOrWhiteSpace(rootDir) ?
                sourceDir :
                sourceDir.Replace(rootDir, string.Empty);

            var sourceDirSegments = path.Split(new[] {Path.DirectorySeparatorChar},
                StringSplitOptions.RemoveEmptyEntries);
            return sourceDirSegments;
        }

        private static bool HasAnyPathSegment(IEnumerable<string> segments, IEnumerable<string> patterns, ILogger logger = null)
        {
            return segments.Any(segment => HasAnyPathSegment(segment, patterns, logger));
        }

        private static bool HasAnyPathSegment(string segment, IEnumerable<string> patterns, ILogger logger = null)
        {
            return patterns.Any(pattern =>
                {
                    bool isMatch = segment.Equals(pattern, StringComparison.InvariantCultureIgnoreCase);

                    if (isMatch)
                    {
                        logger?.WriteDebug($"Segment '{segment}' matches pattern '{pattern}'");
                    }

                    return isMatch;
                });
        }

        private static bool HasAnyPathSegmentStartsWith(IEnumerable<string> segments, IEnumerable<string> patterns)
        {
            return segments.Any(segment => HasAnyPathSegmentStartsWith(segment, patterns));
        }

        private static bool HasAnyPathSegmentStartsWith(string segment, IEnumerable<string> patterns)
        {
            return patterns.Any(pattern => segment.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase));
        }

        private static bool HasAnyPathSegmentPart(IEnumerable<string> segments, IEnumerable<string> patterns)
        {
            return segments.Any(segment => HasAnyPathSegmentPart(segment, patterns));
        }

        private static bool HasAnyPathSegmentPart(string segment, IEnumerable<string> patterns)
        {
            return patterns.Any(pattern => segment.IndexOf(pattern, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }
    }
}
