using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.NuGet;
using FluentAssertions;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit
{
    public class ManifestRewriterTests
    {
        private static string NuSpecNoTags => @"<?xml version=""1.0""?>

<package>
  <metadata>
    <id>Arbor.Build</id>
    <version>0.0.1</version>
    <title>Arbor.Build</title>
    <authors>Niklas Lundberg</authors>
    <owners></owners>
    <description>
      Arbor.Build - convention-based builds
    </description>
    <releaseNotes>
    </releaseNotes>
    <summary>
      Arbor.Build
    </summary>
    <language>en-US</language>
    <projectUrl>https://github.com/niklaslundberg/Arbor.Build</projectUrl>
    <repository type=""git"" url=""https://github.com/niklaslundberg/Arbor.Build"" commit=""$RepositoryCommit$""></repository>
    <iconUrl>https://nuget.org/Content/Images/packageDefaultIcon-50x50.png</iconUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <license type=""expression"">MIT</license>
    <copyright>Copyright Niklas Lundberg</copyright>
    <dependencies>
        <dependency id=""Newtonsoft.Json"" version=""12.0.3"" />
    </dependencies>
    <references></references>
    <tags></tags>
  </metadata>
  <files>

  </files>
</package>
";
        private static string NuSpecNoSourceTags => $@"<?xml version=""1.0""?>

<package>
  <metadata>
    <id>Arbor.Build</id>
    <version>0.0.1</version>
    <title>Arbor.Build</title>
    <authors>Niklas Lundberg</authors>
    <owners></owners>
    <description>
      Arbor.Build - convention-based builds
    </description>
    <releaseNotes>
    </releaseNotes>
    <summary>
      Arbor.Build
    </summary>
    <language>en-US</language>
    <projectUrl>https://github.com/niklaslundberg/Arbor.Build</projectUrl>
    <repository type=""git"" url=""https://github.com/niklaslundberg/Arbor.Build"" commit=""$RepositoryCommit$""></repository>
    <iconUrl>https://nuget.org/Content/Images/packageDefaultIcon-50x50.png</iconUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <license type=""expression"">MIT</license>
    <copyright>Copyright Niklas Lundberg</copyright>
    <dependencies>
        <dependency id=""Newtonsoft.Json"" version=""12.0.3"" />
    </dependencies>
    <references></references>
    <tags>{WellKnownNuGetTags.NoSource}</tags>
  </metadata>
  <files>

  </files>
</package>
";

        public static IEnumerable<object[]> GetFileSystemsData()
        {
            yield return new object[] {new MemoryFileSystem()};
            yield return new object[] {new PhysicalFileSystem()};
        }

        [MemberData(nameof(GetFileSystemsData))]
        [Theory]
        public async Task Rewrite(IFileSystem fileSystem)
        {
            using (fileSystem)
            {
                FileEntry? tempFile = null;

                try
                {
                    tempFile = new FileEntry(fileSystem, UPath.Combine(Path.GetTempPath().AsFullPath(), Guid.NewGuid().ToString(), "my.nuspec"));

                    tempFile.Directory.EnsureExists();

                    await using var stream = tempFile.Open(FileMode.Create, FileAccess.Write);
                    await stream.WriteAllTextAsync(NuSpecNoTags);

                    var manifestReWriter = new ManifestReWriter(fileSystem);

                    var result = await manifestReWriter.Rewrite(tempFile);

                    result.RemoveTags.Should().BeEmpty();
                }
                finally
                {
                    tempFile?.DeleteIfExists();
                    tempFile?.Directory.DeleteIfExists();
                }
            }
        }

        [Fact]
        public async Task RewriteNoSource()
        {
            using IFileSystem fileSystem = new MemoryFileSystem();

            var fileEntry = new FileEntry(fileSystem, new UPath("/my.nuspec"));

            await using (var stream = fileEntry.Open(FileMode.Create, FileAccess.Write))
            {
                await stream.WriteAllTextAsync(NuSpecNoSourceTags);
            }

            var manifestReWriter = new ManifestReWriter(fileSystem);

            var result = await manifestReWriter.Rewrite(fileEntry);

            result.RemoveTags.Should().NotBeEmpty();
        }
    }
}