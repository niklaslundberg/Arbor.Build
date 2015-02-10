using System.Collections.Generic;

namespace Arbor.X.Core.Tools
{
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