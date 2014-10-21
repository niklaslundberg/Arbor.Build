using System.Collections.Generic;
using Arbor.Sorbus.Core;

namespace Arbor.X.Core.Tools
{
    public class PathLookupSpecification
    {
        readonly IReadOnlyCollection<string> _ignoredFilePatterns;
        readonly IReadOnlyCollection<string> _ignoredDirectorySegments;
        readonly IReadOnlyCollection<string> _ignoredDirectorySegmentParts;

        public PathLookupSpecification(IEnumerable<string> ignoredDirectorySegments = null, IEnumerable<string> ignoredFilePatterns = null, IEnumerable<string> ignoredDirectorySegmentParts = null)
        {
            _ignoredFilePatterns = (ignoredFilePatterns ?? new List<string>()).ToReadOnly();
            _ignoredDirectorySegments = (ignoredDirectorySegments ?? new List<string>()).ToReadOnly();
            _ignoredDirectorySegmentParts = (ignoredDirectorySegmentParts ?? new List<string>()).ToReadOnly();
        }

        public IReadOnlyCollection<string> IgnoredFilePatterns
        {
            get { return _ignoredFilePatterns; }
        }

        public IReadOnlyCollection<string> IgnoredDirectorySegments
        {
            get { return _ignoredDirectorySegments; }
        }

        public IReadOnlyCollection<string> IgnoredDirectorySegmentParts
        {
            get { return _ignoredDirectorySegmentParts; }
        }
    }
}