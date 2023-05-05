using System.Collections.Generic;
using Arbor.Build.Core.Tools.Versioning;
using NuGet.Versioning;
using Xunit;

namespace Arbor.Build.Tests.Integration.Versioning;

public class SemVerExtensionSpecification
{
    public static IEnumerable<object[]> Data()
    {
        yield return new object[] { "1.2.3-abc", "abc" };
        yield return new object[] { "1.2.3-build00001", "build00001" };
        yield return new object[] { "1.2.3-abc+123", "abc+123" };
        yield return new object[] { "1.2.3-abc.1.2.3+def", "abc.1.2.3+def" };
        yield return new object[] { "1.2.3-build.4", "build.4" };
    }

    [Theory]
    [MemberData(nameof(Data))]
    public void GetSuffixShouldReturnSuffix(string semver, string expected)
    {
        SemanticVersion semanticVersion = SemanticVersion.Parse(semver);

        string suffix = semanticVersion.Suffix();

        Assert.Equal(expected, suffix);
    }
}