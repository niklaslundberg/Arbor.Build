using System;
using System.Collections.Immutable;
using System.IO;
using Arbor.Build.Core.IO;
using Xunit;

namespace Arbor.Build.Tests.Integration.DirectoryExtensions
{
    public class DirectoryListFilesTest
    {
        [Fact]
        public void WhenGettingFilesExcludingUserFiles()
        {
            DirectoryInfo tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                .EnsureExists();

            DirectoryInfo subDirectoryA = tempDirectory.CreateSubdirectory("Abc");

            string fileName = Path.Combine(subDirectoryA.FullName, "def.txt");
            using (File.Create(fileName))
            {
            }

            string userFile = Path.Combine(subDirectoryA.FullName, "def.user");
            using (File.Create(userFile))
            {
            }

            ImmutableArray<string> files = tempDirectory.GetFilesWithWithExclusions(new[] { "*.user" });

            tempDirectory.DeleteIfExists();

            Assert.Contains(fileName, files);
            Assert.DoesNotContain(userFile, files);
            Assert.Single(files);
        }
    }
}
