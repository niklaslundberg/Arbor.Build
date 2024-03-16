using System.Collections.Generic;
using System.Linq;
using Arbor.Defensive.Collections;

namespace Arbor.Build.Core.IO;

public static class PathSpecificationExtensions
{
    public static PathLookupSpecification WithIgnoredFileNameParts(
        this PathLookupSpecification pathLookupSpecification,
        IEnumerable<string> ignoredFileNameParts) => new(
        pathLookupSpecification.IgnoredDirectorySegments,
        pathLookupSpecification.IgnoredFileStartsWithPatterns,
        pathLookupSpecification.IgnoredDirectorySegmentParts,
        pathLookupSpecification.IgnoredDirectoryStartsWithPatterns,
        ignoredFileNameParts.SafeToReadOnlyCollection());

    public static PathLookupSpecification AddExcludedDirectorySegments(
        this PathLookupSpecification pathLookupSpecification,
        IEnumerable<string> ignoredExcludedDirectorySegments) => new(
        pathLookupSpecification.IgnoredDirectorySegments.Concat(ignoredExcludedDirectorySegments),
        pathLookupSpecification.IgnoredFileStartsWithPatterns,
        pathLookupSpecification.IgnoredDirectorySegmentParts,
        pathLookupSpecification.IgnoredDirectoryStartsWithPatterns,
        pathLookupSpecification.IgnoredFileNameParts);
}