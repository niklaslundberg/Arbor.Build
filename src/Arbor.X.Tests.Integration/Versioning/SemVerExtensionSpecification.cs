using System.Collections.Generic;
using Arbor.X.Core.Tools.NuGet;
using Arbor.X.Core.Tools.Versioning;
using NuGet.Versioning;
using Xunit;

namespace Arbor.X.Tests.Integration.Versioning
{
    public class SemVerExtensionSpecification
    {
        [Theory]
        [MemberData(nameof(Data))]
        public void GetSuffixShouldReturnSuffix(string semver, string expected)
        {
            SemanticVersion semanticVersion = SemanticVersion.Parse(semver);

            string suffix = semanticVersion.Suffix();

            Assert.Equal(expected, suffix);
        }

        public static IEnumerable<object[]> Data()
        {
            yield return new object[] {"1.2.3-abc", "abc"};
            yield return new object[] {"1.2.3-build00001", "build00001"};
            yield return new object[] {"1.2.3-abc+123", "abc+123"};
            yield return new object[] {"1.2.3-abc.1.2.3+def", "abc.1.2.3+def"};
            yield return new object[] {"1.2.3-build.4", "build.4"};
        }
    }

    public class NuGetVersionHelperTests
    {
        [Fact]
        public void Should()
        {
            string version = NuGetVersionHelper.GetVersion("1.2.3.4", false, "build", true, null, null, NuGetVersioningSettings.Default);

            Assert.Equal("1.2.3-build.4", version);
        }

    }
}
