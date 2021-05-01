using System;
using System.Collections.Generic;

namespace Arbor.Build.Core.IO
{
    public static class DefaultPaths
    {
        public const string TempPathPrefix = "ABX";

        private static readonly Lazy<PathLookupSpecification> PathLookupSpecification =
            new Lazy<PathLookupSpecification>(Initialize);

        public static PathLookupSpecification DefaultPathLookupSpecification => PathLookupSpecification.Value;

        private static PathLookupSpecification Initialize()
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
                ".sonar",
                ".userprefs",
                "node_modules",
                "bower_components"
            };

            var ignoredFileStartsWithPatterns = new List<string>(10) { ".", "_", "ncrunchTemp_" };

            var ignoredDirectorySegmentParts = new List<string>(5) {"_"};

            var ignoredDirectoryStartsWithPatterns = new List<string>(10) { "_" };

            return new PathLookupSpecification(
                ignoredDirectorySegments,
                ignoredFileStartsWithPatterns,
                ignoredDirectorySegmentParts,
                ignoredDirectoryStartsWithPatterns);
        }
    }
}
