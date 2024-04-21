using Arbor.Build.Core;
using FluentAssertions;
using Xunit;

namespace Arbor.Build.Tests.Unit
{
    public class ConstantTests
    {
        [Fact]
        public void PackageNameShouldBeArborBuild() => ArborConstants.ArborPackageName.Should().Be("Arbor.Build");
    }
}