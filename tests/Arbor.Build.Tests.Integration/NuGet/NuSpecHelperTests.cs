using System;
using System.IO;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.FS;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.NuGet
{
    public class NuSpecHelperTests
    {
        public NuSpecHelperTests(ITestOutputHelper output) => _output = output;

        readonly ITestOutputHelper _output;

        [Fact]
        public void WhenGettingIncludedFiles()
        {
            using var fs = new PhysicalFileSystem();
            var tempPath = Path.GetTempPath().AsFullPath();

            DirectoryEntry tempDirectory = new DirectoryEntry(fs, UPath.Combine(tempPath, Guid.NewGuid().ToString()))
                .EnsureExists();

            DirectoryEntry subDirectoryA = tempDirectory.CreateSubdirectory("Abc");

            UPath fileName = UPath.Combine(subDirectoryA.FullName, "def.txt");


            using (fs.CreateFile(fileName))
            {
            }

            var includedFile = NuSpecHelper.IncludedFile(new FileEntry(fs, fileName), tempDirectory, Logger.None);

            tempDirectory.DeleteIfExists();

            Assert.NotNull(includedFile);

            _output.WriteLine(includedFile);
        }
    }
}
