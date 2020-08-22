using System.IO;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using FluentAssertions;
using Xunit;

namespace Arbor.Build.Tests.Integration
{
    public class SolutionProjectExtensionsTests
    {
        [Fact]
        public void TestProjectShouldHavePublishedDisabled()
        {
            var msbuildProject = MSBuildProject.LoadFrom(Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "tests",
                "Arbor.Build.Tests.Integration", "Arbor.Build.Tests.Integration.csproj"));
            var project = new SolutionProject("path", "name", "dir", msbuildProject,
                NetFrameworkGeneration.NetCoreApp);

            project.PublishEnabled().Should().BeFalse();
        }
    }
}