using System.Collections.Generic;
using System.Collections.Immutable;
using Arbor.Defensive.Collections;

namespace Arbor.X.Core.IO
{
    public class PathLookupSpecification
    {
        public PathLookupSpecification(
            IEnumerable<string> ignoredDirectorySegments = null,
            IEnumerable<string> ignoredFileStartsWithPatterns = null,
            IEnumerable<string> ignoredDirectorySegmentParts = null,
            IEnumerable<string> ignoredDirectoryStartsWithPatterns = null,
            IEnumerable<string> ignoredFileNameParts = null)
        {
            IgnoredFileStartsWithPatterns = ignoredFileStartsWithPatterns.SafeToReadOnlyCollection();
            IgnoredDirectorySegments = ignoredDirectorySegments.SafeToReadOnlyCollection();
            IgnoredDirectorySegmentParts = ignoredDirectorySegmentParts.SafeToReadOnlyCollection();
            IgnoredDirectoryStartsWithPatterns = ignoredDirectoryStartsWithPatterns.SafeToReadOnlyCollection();
            IgnoredFileNameParts = ignoredFileNameParts.SafeToReadOnlyCollection();
        }

        public ImmutableArray<string> IgnoredFileStartsWithPatterns { get; }

        public ImmutableArray<string> IgnoredDirectoryStartsWithPatterns { get; }

        public ImmutableArray<string> IgnoredDirectorySegments { get; }

        public ImmutableArray<string> IgnoredDirectorySegmentParts { get; }

        public ImmutableArray<string> IgnoredFileNameParts { get; }
    }
}
