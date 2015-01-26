using System.Collections.Generic;
using Arbor.Sorbus.Core;

namespace Arbor.X.Core.Tools
{
    public class PathLookupSpecification
    {
        readonly IReadOnlyCollection<string> _ignoredFileStartsWithPatterns;
        readonly IReadOnlyCollection<string> _ignoredDirectorySegments;
        readonly IReadOnlyCollection<string> _ignoredDirectorySegmentParts;
        readonly IReadOnlyCollection<string> _ignoredDirectoryStartsWithPatterns;

        public PathLookupSpecification(
            IEnumerable<string> ignoredDirectorySegments = null, 
            IEnumerable<string> ignoredFileStartsWithPatterns = null, 
            IEnumerable<string> ignoredDirectorySegmentParts = null,
            IEnumerable<string> ignoredDirectoryStartsWithPatterns = null)
        {
            _ignoredFileStartsWithPatterns = (ignoredFileStartsWithPatterns ?? new List<string>()).ToReadOnly();
            _ignoredDirectorySegments = (ignoredDirectorySegments ?? new List<string>()).ToReadOnly();
            _ignoredDirectorySegmentParts = (ignoredDirectorySegmentParts ?? new List<string>()).ToReadOnly();
            _ignoredDirectoryStartsWithPatterns = (ignoredDirectoryStartsWithPatterns ?? new List<string>()).ToReadOnly();
        }

        public IReadOnlyCollection<string> IgnoredFileStartsWithPatterns
        {
            get { return _ignoredFileStartsWithPatterns; }
        }
        public IReadOnlyCollection<string> IgnoredDirectoryStartsWithPatterns
        {
            get { return _ignoredDirectoryStartsWithPatterns; }
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


    public static class DefaultPaths
    {
        public static PathLookupSpecification DefaultPathLookupSpecification
        {
            get
            {
                List<string> ignoredDirectorySegments = new List<string>(20) { "bin", "obj", ".git", ".hg", ".svn", "TestResults", "_ReSharper", ".HistoryData", "LocalHistory" };
                List<string> ignoredFileStartsWithPatterns = new List<string>(10) {".", "_"};
                List<string> ignoredDirectorySegmentParts = new List<string>(5);
                List<string> ignoredDirectoryStartsWithPatterns = new List<string>(10) {"_"};
                return new PathLookupSpecification(ignoredDirectorySegments, ignoredFileStartsWithPatterns, ignoredDirectorySegmentParts, ignoredDirectoryStartsWithPatterns);
            }
        }
    }
}