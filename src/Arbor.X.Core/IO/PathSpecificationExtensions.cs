using System.Collections.Generic;
using System.Linq;
using Arbor.Defensive.Collections;

namespace Arbor.X.Core.IO
{
    public static class PathSpecificationExtensions
    {
        public static PathLookupSpecification WithIgnoredFileNameParts(
            this PathLookupSpecification pathLookupSpecification,
            IEnumerable<string> ignoredFileNameParts)
        {
            return new PathLookupSpecification(
                pathLookupSpecification.IgnoredDirectorySegments,
                pathLookupSpecification.IgnoredFileStartsWithPatterns,
                pathLookupSpecification.IgnoredDirectorySegmentParts,
                pathLookupSpecification.IgnoredDirectoryStartsWithPatterns,
                ignoredFileNameParts.SafeToReadOnlyCollection());
        }

        public static PathLookupSpecification AddExcludedDirectorySegments(
            this PathLookupSpecification pathLookupSpecification,
            IEnumerable<string> ignoredExcludedDirectorySegments)
        {
            return new PathLookupSpecification(
                pathLookupSpecification.IgnoredDirectorySegments.Concat(ignoredExcludedDirectorySegments),
                pathLookupSpecification.IgnoredFileStartsWithPatterns,
                pathLookupSpecification.IgnoredDirectorySegmentParts,
                pathLookupSpecification.IgnoredDirectoryStartsWithPatterns,
                pathLookupSpecification.IgnoredFileNameParts);
        }
    }
}
