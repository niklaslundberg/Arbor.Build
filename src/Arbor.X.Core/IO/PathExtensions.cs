using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            var ignoredFileNameParts = pathLookupSpecification.IignoredFileNameParts.Where(part => !string.IsNullOrEmpty(part)).Where(
                part => sourceFileInfo.Name.IndexOf(part, StringComparison.InvariantCultureIgnoreCase) >= 0).SafeToReadOnlyCollection();

            isBlackListed = isBlackListed || ignoredFileNameParts.Any();

            if (ignoredFileNameParts.Any())
            {
                logger?.WriteDebug($"Ignored file name parts of '{sourceFile}' makes it blacklisted: {string.Join(", ", ignoredFileNameParts.Select(item => $"'{item}'"))}");
            }

            return isBlackListed;
        }

        public static bool IsBlackListed(this PathLookupSpecification pathLookupSpecification, string sourceDir, string rootDir = null)
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
                pathLookupSpecification.IgnoredDirectorySegments);

            if (hasAnyPathSegment)
            {
                return true;
            }

            bool hasAnyPathSegmentPart = HasAnyPathSegmentPart(sourceDirSegments,
                pathLookupSpecification.IgnoredDirectorySegmentParts);

            if (hasAnyPathSegmentPart)
            {
                return true;
            }

            bool hasAnyPartStartsWith = HasAnyPathSegmentStartsWith(sourceDirSegments,
                pathLookupSpecification.IgnoredDirectoryStartsWithPatterns);

            if (hasAnyPartStartsWith)
            {
                return true;
            }

            return false;
        }

        private static string[] GetSourceDirSegments(string sourceDir, string rootDir)
        {
            var path = string.IsNullOrWhiteSpace(rootDir) ?
                sourceDir :
                sourceDir.Replace(rootDir, "");

            var sourceDirSegments = path.Split(new[] {Path.DirectorySeparatorChar},
                StringSplitOptions.RemoveEmptyEntries);
            return sourceDirSegments;
        }

        static bool HasAnyPathSegment(IEnumerable<string> segments, IEnumerable<string> patterns)
        {
            return segments.Any(segment => HasAnyPathSegment(segment, patterns));
        }

        static bool HasAnyPathSegment(string segment, IEnumerable<string> patterns)
        {
            return patterns.Any(pattern => segment.Equals(pattern, StringComparison.InvariantCultureIgnoreCase));
        }

        static bool HasAnyPathSegmentStartsWith(IEnumerable<string> segments, IEnumerable<string> patterns)
        {
            return segments.Any(segment => HasAnyPathSegmentStartsWith(segment, patterns));
        }

        static bool HasAnyPathSegmentStartsWith(string segment, IEnumerable<string> patterns)
        {
            return patterns.Any(pattern => segment.StartsWith(pattern, StringComparison.InvariantCultureIgnoreCase));
        }

        static bool HasAnyPathSegmentPart(IEnumerable<string> segments, IEnumerable<string> patterns)
        {
            return segments.Any(segment => HasAnyPathSegmentPart(segment, patterns));
        }

        static bool HasAnyPathSegmentPart(string segment, IEnumerable<string> patterns)
        {
            return patterns.Any(pattern => segment.IndexOf(pattern, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }
    }
}
