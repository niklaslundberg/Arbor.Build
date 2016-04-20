using System.Collections.Generic;

using Arbor.X.Core.GenericExtensions;

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
    }
}
