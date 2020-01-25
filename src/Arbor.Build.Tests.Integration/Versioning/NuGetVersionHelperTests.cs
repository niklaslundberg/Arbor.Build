using Arbor.Build.Core.Tools.NuGet;
using Xunit;

namespace Arbor.Build.Tests.Integration.Versioning
{
    public class NuGetVersionHelperTests
    {
        [Fact]
        public void Should()
        {
            string version = NuGetVersionHelper.GetVersion("1.2.3.4",
                false,
                "build",
                true,
                null,
                null,
                NuGetVersioningSettings.Default);

            Assert.Equal("1.2.3-build.4", version);
        }
    }
}