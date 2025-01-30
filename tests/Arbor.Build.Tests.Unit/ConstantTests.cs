using Arbor.Build.Core;
using Shouldly;
using Xunit;

namespace Arbor.Build.Tests.Unit
{
    public class ConstantTests
    {
        [Fact]
        public void PackageNameShouldBeArborBuild() => ArborConstants.ArborPackageName.ShouldBe("Arbor.Build");
    }
}