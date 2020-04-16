using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Core.Tools.NuGet;
using Xunit;

namespace Arbor.Build.Tests.Unit.NuGetTests
{
    public class NuGetPackageConfigurationTests
    {
        [Fact]
        public void BuildVersionShouldBeIncludedInVersionWhenUsingDefault()
        {
            string packageVersion = NuGetVersionHelper.GetPackageVersion(new VersionOptions("1.2.3.4"));

            Assert.Equal("1.2.3-build.4", packageVersion);
        }

        [Fact]
        public void SubVersionShouldNotBeUsedForSemVer1()
        {
            string packageVersion = NuGetVersionHelper.GetPackageVersion(new VersionOptions("1.2.3.4")
            {
                NuGetVersioningSettings = new NuGetVersioningSettings
                {
                    SemVerVersion = 1
                }
            });

            Assert.Equal("1.2.3-build4", packageVersion);
        }

        [Fact]
        public void LeadingZerosShouldBeUsedWhenDefined()
        {
            string packageVersion = NuGetVersionHelper.GetPackageVersion(new VersionOptions("1.2.3.4")
            {
                NuGetVersioningSettings = new NuGetVersioningSettings
                {
                    SemVerVersion = 1,
                    MaxZeroPaddingLength = 4
                }
            });

            Assert.Equal("1.2.3-build0004", packageVersion);
        }

        [Fact]
        public void SuffixShouldNotBeIncludedInVersionWhenSuffixIsEmptyString()
        {
            string packageVersion = NuGetVersionHelper.GetPackageVersion(new VersionOptions("1.2.3.4")
            {
                BuildSuffix = ""
            });

            Assert.Equal("1.2.3-4", packageVersion);
        }

        [Fact]
        public void SuffixShouldBeUsedIncludedInVersionWhenDefined()
        {
            string packageVersion = NuGetVersionHelper.GetPackageVersion(new VersionOptions("1.2.3.4")
            {
                BuildSuffix = "custom",
                BuildNumberEnabled = true,
                IsReleaseBuild = false
            });

            Assert.Equal("1.2.3-custom.4", packageVersion);
        }

        [Fact]
        public void ReleaseBuildByDefaultShouldStripBuildNumber()
        {
            string packageVersion = NuGetVersionHelper.GetPackageVersion(new VersionOptions("1.2.3.4")
            {
                IsReleaseBuild = true
            });

            Assert.Equal("1.2.3", packageVersion);
        }

        [Fact]
        public void BuildSuffixShouldBeIncludedInVersionWhenUsingGitFlowMasterForNonRelease()
        {
            string packageVersion = NuGetVersionHelper.GetPackageVersion(new VersionOptions("1.2.3.4")
            {
                IsReleaseBuild = false,
                GitModel = GitModel.GitFlowBuildOnMaster
            });

            Assert.Equal("1.2.3-build.4", packageVersion);
        }

        [Fact]
        public void RcSuffixShouldBeIncludedInVersionWhenUsingGitFlowMasterForRelease()
        {
            string packageVersion = NuGetVersionHelper.GetPackageVersion(new VersionOptions("1.2.3.4")
            {
                IsReleaseBuild = true,
                GitModel = GitModel.GitFlowBuildOnMaster
            });

            Assert.Equal("1.2.3-rc.4", packageVersion);
        }
    }
}
