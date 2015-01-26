using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arbor.X.Core.Tools
{
    public static class PathExtensions
    {
        public static bool IsFileBlackListed(this PathLookupSpecification pathLookupSpecification, string sourceFile)
        {
            if (pathLookupSpecification == null)
            {
                throw new ArgumentNullException("pathLookupSpecification");
            }

            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                throw new ArgumentNullException("sourceFile");
            }
            
            if (!File.Exists(sourceFile))
            {
                return true;
            }

            var sourceFileInfo = new FileInfo(sourceFile);

            if (pathLookupSpecification.IsBlackListed(sourceFileInfo.Directory.FullName))
            {
                return true;
            }
            
            var isBlackListed = HasAnyPathSegmentStartsWith(sourceFileInfo.Name, pathLookupSpecification.IgnoredFileStartsWithPatterns);

            return isBlackListed;
        }
        public static bool IsBlackListed(this PathLookupSpecification pathLookupSpecification, string sourceDir)
        {
            if (pathLookupSpecification == null)
            {
                throw new ArgumentNullException("pathLookupSpecification");
            }

            if (string.IsNullOrWhiteSpace(sourceDir))
            {
                throw new ArgumentNullException("sourceDir");
            }

            if (!Directory.Exists(sourceDir))
            {
                return true;
            }

            var sourceDirSegments = sourceDir.Split(new[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);

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