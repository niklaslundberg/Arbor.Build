using System.Collections.Generic;

using Arbor.X.Core.GenericExtensions;

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
            IignoredFileNameParts = ignoredFileNameParts.SafeToReadOnlyCollection();
        }

        public IReadOnlyCollection<string> IgnoredFileStartsWithPatterns { get; }

        public IReadOnlyCollection<string> IgnoredDirectoryStartsWithPatterns { get; }

        public IReadOnlyCollection<string> IgnoredDirectorySegments { get; }

        public IReadOnlyCollection<string> IgnoredDirectorySegmentParts { get; }

        public IReadOnlyCollection<string> IignoredFileNameParts { get; }
    }
}
