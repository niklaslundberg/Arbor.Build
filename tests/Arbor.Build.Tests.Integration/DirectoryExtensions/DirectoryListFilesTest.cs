using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Arbor.Build.Core.IO;
using Arbor.FS;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.DirectoryExtensions
{
    public class DirectoryListFilesTest
    {
        [Fact]
        public void WhenGettingFilesExcludingUserFiles()
        {
            var fs = new WindowsFs(new PhysicalFileSystem());
            DirectoryEntry tempDirectory = new DirectoryEntry(fs,Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                .EnsureExists();

            var subDirectoryA = tempDirectory.CreateSubdirectory("Abc");

            string fileName = Path.Combine(subDirectoryA.FullName, "def.txt");
            using (File.Create(fileName))
            {
            }

            string userFile = Path.Combine(subDirectoryA.FullName, "def.user");
            using (File.Create(userFile))
            {
            }

            ImmutableArray<string> files = tempDirectory.GetFilesWithWithExclusions(fs,new[] { "*.user" }).Select(file => file.FullName).ToImmutableArray();

            tempDirectory.DeleteIfExists();

            Assert.Contains(fileName, files);
            Assert.DoesNotContain(userFile, files);
            Assert.Single(files);
        }
    }
}
