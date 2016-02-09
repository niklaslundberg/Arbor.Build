using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arbor.X.Core.IO
{
    public static class PathExtensions
    {
        public static bool IsFileBlackListed(this PathLookupSpecification pathLookupSpecification, string sourceFile, string rootDir = null)
        {
            if (pathLookupSpecification == null)
            {
                throw new ArgumentNullException(nameof(pathLookupSpecification));
            }

            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }
            
            if (!File.Exists(sourceFile))
            {
                return true;
            }

            var sourceFileInfo = new FileInfo(sourceFile);

            if (pathLookupSpecification.IsBlackListed(sourceFileInfo.Directory.FullName, rootDir))
            {
                return true;
            }
            
            var isBlackListed = HasAnyPathSegmentStartsWith(sourceFileInfo.Name, pathLookupSpecification.IgnoredFileStartsWithPatterns);

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
