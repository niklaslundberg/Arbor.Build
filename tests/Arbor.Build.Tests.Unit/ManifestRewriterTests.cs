using System.Text;
using Arbor.Build.Core.Tools.NuGet;
using FluentAssertions;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit
{
    public class ManifestRewriterTests
    {
        private string NuSpecNoTags => @"<?xml version=""1.0""?>

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
        private string NuSpecNoSourceTags => $@"<?xml version=""1.0""?>

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

        [Fact]
        public void Rewrite()
        {
            IFileSystem fileSystem = new MemoryFileSystem();

            var path = new UPath("/my.nuspec");
            fileSystem.WriteAllText(path, NuSpecNoTags, Encoding.UTF8);

            var manifestReWriter = new ManifestReWriter(fileSystem);

            var result = manifestReWriter.Rewrite(path.FullName);

            result.RemoveTags.Should().BeEmpty();
        }

        [Fact]
        public void RewriteNoSource()
        {
            IFileSystem fileSystem = new MemoryFileSystem();

            var path = new UPath("/my.nuspec");
            fileSystem.WriteAllText(path, NuSpecNoSourceTags, Encoding.UTF8);

            var manifestReWriter = new ManifestReWriter(fileSystem);

            var result = manifestReWriter.Rewrite(path.FullName);

            result.RemoveTags.Should().NotBeEmpty();
        }
    }
}