using System.Collections.Generic;

namespace Arbor.X.Core.IO
{
    public static class DefaultPaths
    {
        public static PathLookupSpecification DefaultPathLookupSpecification
        {
            get
            {
                var ignoredDirectorySegments = new List<string>(20)
                {
                    "bin",
                    "obj",
                    ".git",
                    ".hg",
                    ".svn",
                    "TestResults",
                    "_ReSharper",
                    ".HistoryData",
                    "LocalHistory",
                    "packages",
                    "temp",
                    "artifacts",
                    ".vs",
                    ".user",
                    ".userprefs",
                    "node_modules",
                    "bower_components"
                };

                var ignoredFileStartsWithPatterns = new List<string>(10) { ".", "_", "ncrunchTemp_" };

                var ignoredDirectorySegmentParts = new List<string>(5);

                var ignoredDirectoryStartsWithPatterns = new List<string>(10) { "_" };

                return new PathLookupSpecification(
                    ignoredDirectorySegments,
                    ignoredFileStartsWithPatterns,
                    ignoredDirectorySegmentParts,
                    ignoredDirectoryStartsWithPatterns);
            }
        }

        public const string TempPathPrefix = "ABX";
    }
}
