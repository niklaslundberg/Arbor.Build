using System.Threading.Tasks;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Arbor.FS;
using FluentAssertions;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration
{
    public class SolutionProjectExtensionsTests
    {
        [Fact]
        public async Task TestProjectShouldHavePublishedDisabled()
        {
            using var fs = new PhysicalFileSystem();
            var projectFileFullName = fs.GetFileEntry(UPath.Combine(VcsTestPathHelper.FindVcsRootPath().Path, "tests",
                "Arbor.Build.Tests.Integration", "Arbor.Build.Tests.Integration.csproj"));
            var msbuildProject = await MSBuildProject.LoadFrom(projectFileFullName);

            var solutionFile = fs.GetFileEntry(UPath.Combine(projectFileFullName.Parent.Parent.Parent.Path, "Arbor.Build.sln"));

            var project = new SolutionProject(solutionFile, "name", msbuildProject.ProjectDirectory, msbuildProject,
                NetFrameworkGeneration.NetCoreApp);

            project.PublishEnabled().Should().BeFalse();
        }
    }
}