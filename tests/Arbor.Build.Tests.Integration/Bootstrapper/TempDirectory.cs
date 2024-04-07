using System;
using System.IO;
using Arbor.FS;
using Zio;

namespace Arbor.Build.Tests.Integration.Bootstrapper
{
    public class TempDirectory : IDisposable
    {
        public DirectoryEntry Directory { get; }

        private TempDirectory(DirectoryEntry tempDirectory) => Directory = tempDirectory;

        public static TempDirectory Create(IFileSystem fs)
        {
            var tempPath = fs.ConvertPathFromInternal(Path.GetTempPath());

            var tempDirectory = new DirectoryEntry(fs, tempPath / Guid.NewGuid().ToString());

            tempDirectory.Create();

            return new TempDirectory(tempDirectory);
        }

        public void Dispose() => Directory.DeleteIfExists();
    }
}