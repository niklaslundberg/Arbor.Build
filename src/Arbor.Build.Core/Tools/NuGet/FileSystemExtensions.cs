using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class FileSystemExtensions
    {
        public static StringComparison GetPathComparison(this IFileSystem _) =>
            StringComparison.OrdinalIgnoreCase; // TODO support multiple file systems

        public static FileEntry[] GetFiles(this DirectoryEntry directoryEntry,
            string? searchPattern = null,
            SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            directoryEntry.EnumerateFiles(searchPattern, searchOption).ToArray();
        public static IEnumerable<DirectoryEntry> GetDirectories(this DirectoryEntry directoryEntry,
            string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly) =>
            directoryEntry.EnumerateDirectories(searchPattern, searchOption);
    }
}