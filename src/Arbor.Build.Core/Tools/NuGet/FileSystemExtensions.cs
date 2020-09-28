using System;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class FileSystemExtensions
    {
        public static StringComparison GetPathComparison(this IFileSystem _) => StringComparison.OrdinalIgnoreCase; // TODO support multiple file systems
    }
}