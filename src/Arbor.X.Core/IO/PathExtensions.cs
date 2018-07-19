using System; using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.Defensive.Collections;

namespace Arbor.X.Core.IO
{
    public static class PathExtensions
    {
        public static (bool, string) IsFileBlackListed(
            this PathLookupSpecification pathLookupSpecification,
            string sourceFile,
            string rootDir = null,
            bool allowNonExistingFiles = false,
            ILogger logger = null)
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
                logger?.Debug(messageMessage);
                return (true, messageMessage);
            }

            var sourceFileInfo = new FileInfo(sourceFile);

            (bool, string) directoryBlackListed = pathLookupSpecification.IsBlackListed(sourceFileInfo.Directory.FullName, rootDir);

            if (directoryBlackListed.Item1)
            {
                string reasonMessage = $"Directory of '{sourceFile}' is blacklisted, {directoryBlackListed.Item2}";
                logger?.Debug(reasonMessage);
                return (true, reasonMessage);
            }

            bool isBlackListed = HasAnyPathSegmentStartsWith(
                sourceFileInfo.Name,
                pathLookupSpecification.IgnoredFileStartsWithPatterns);

            if (isBlackListed)
            {
                string reasonMessage = $"Path segments of '{sourceFile}' makes it blacklisted";
                logger?.Debug(reasonMessage);
                return (true, reasonMessage);
            }

            IReadOnlyCollection<string> ignoredFileNameParts = pathLookupSpecification.IgnoredFileNameParts
                .Where(part => !string.IsNullOrEmpty(part))
                .Where(
                    part => sourceFileInfo.Name.IndexOf(part, StringComparison.InvariantCultureIgnoreCase) >= 0)
                .SafeToReadOnlyCollection();

            if (ignoredFileNameParts.Count > 0)
            {
                string reasonMessage = $"Ignored file name parts of '{sourceFile}' makes it blacklisted: {string.Join(", ", ignoredFileNameParts.Select(item => $"'{item}'"))}";
                logger?.Debug(reasonMessage);
                return (true, reasonMessage);
            }

            return (false, string.Empty);
        }

        public static (bool, string) IsBlackListed(
            this PathLookupSpecification pathLookupSpecification,
            string sourceDir,
            string rootDir = null,
            ILogger logger = null)
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
                string reasonMessage = $"The directory '{sourceDir}' has a path segment that is blacklisted";
                logger?.Debug(reasonMessage);
                return (true, reasonMessage);
            }

            bool hasAnyPathSegmentPart = HasAnyPathSegmentPart(
                sourceDirSegments,
                pathLookupSpecification.IgnoredDirectorySegmentParts);

            if (hasAnyPathSegmentPart)
            {
                string reasonMessage = $"The directory '{sourceDir}' has a path segment part that is blacklisted";
                logger?.Debug(reasonMessage);
                return (true, reasonMessage);
            }

            bool hasAnyPartStartsWith = HasAnyPathSegmentStartsWith(
                sourceDirSegments,
                pathLookupSpecification.IgnoredDirectoryStartsWithPatterns);

            if (hasAnyPartStartsWith)
            {
                string reasonMessage = $"The directory '{sourceDir}' has a path that starts with a pattern that is blacklisted";
                logger?.Debug(
                    reasonMessage);
                return (true, reasonMessage);
            }

            logger?.Debug("The directory '{SourceDir}' is not blacklisted", sourceDir);

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
            ILogger logger = null)
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
                    logger?.Debug("Segment '{Segment}' matches pattern '{Pattern}'", segment, pattern);
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
