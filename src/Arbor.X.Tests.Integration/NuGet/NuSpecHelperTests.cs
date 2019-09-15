using System;
using System.IO;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.NuGet;
using Xunit;
using Xunit.Abstractions;

namespace Arbor.Build.Tests.Integration.NuGet
{
    public class NuSpecHelperTests
    {
        public NuSpecHelperTests(ITestOutputHelper output) => _output = output;

        readonly ITestOutputHelper _output;

        [Fact]
        public void WhenGettingIncludedFiles()
        {
            DirectoryInfo tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                .EnsureExists();

            DirectoryInfo subDirectoryA = tempDirectory.CreateSubdirectory("Abc");

            string fileName = Path.Combine(subDirectoryA.FullName, "def.txt");
            using (File.Create(fileName))
            {
            }

            string includedFile = NuSpecHelper.IncludedFile(fileName, tempDirectory.FullName);

            tempDirectory.DeleteIfExists();

            Assert.NotNull(includedFile);

            _output.WriteLine(includedFile);
        }
    }
}
