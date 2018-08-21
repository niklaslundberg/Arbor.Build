using Arbor.Build.Core.Tools.NuGet;
using NuGet.Versioning;
using Serilog.Core;
using Xunit;

namespace Arbor.Build.Tests.Integration.NuGet
{
    public class GetVersion
    {
        [Fact]
        public void Spec()
        {
            string version = NuGetVersionHelper.GetVersion("1.2.3.4",
                false,
                "build",
                true,
                null,
                Logger.None,
                NuGetVersioningSettings.Default);

            Assert.Equal("1.2.3-build.4", version);
        }

        [Fact]
        public void SpecParsed()
        {
            string version = NuGetVersionHelper.GetVersion("1.2.3.4",
                false,
                "build",
                true,
                null,
                Logger.None,
                NuGetVersioningSettings.Default);

            SemanticVersion semver = SemanticVersion.Parse(version);

            Assert.Equal("1.2.3-build.4", semver.ToNormalizedString());
        }
    }
}
