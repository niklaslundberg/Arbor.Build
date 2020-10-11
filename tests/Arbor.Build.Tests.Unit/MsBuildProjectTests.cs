using System.Threading.Tasks;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.MSBuild;
using FluentAssertions;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit
{
    public class MsBuildProjectTests
    {
        [Fact]
        public async Task WhenProjectHasSingleTargetFrameworkTheValueShouldNotBeNull()
        {
            using IFileSystem fileSystem = new MemoryFileSystem();

            UPath path = "/test.csproj";
            await using (var stream = fileSystem.CreateFile(path))
            {
                await stream.WriteAllTextAsync(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
</Project>");
            }

            var file = new FileEntry(fileSystem, path);

            var msBuildProject = await MsBuildProject.LoadFrom(file);

            msBuildProject.TargetFramework.Should().Be(TargetFramework.NetStandard2_0);
            msBuildProject.TargetFrameworks.Length.Should().Be(1);
        }

        [Fact]
        public async Task WhenProjectHasMultipleTargetFrameworksTheTargetShouldNotBeEmpty()
        {
            using IFileSystem fileSystem = new MemoryFileSystem();

            UPath path = "/test.csproj";
            await using (var stream = fileSystem.CreateFile(path))
            {
                await stream.WriteAllTextAsync(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;netcoreapp3.1</TargetFrameworks>
  </PropertyGroup>
</Project>");
            }


            var file = new FileEntry(fileSystem, path);

            var msBuildProject = await MsBuildProject.LoadFrom(file);

            msBuildProject.TargetFramework.Should().Be(TargetFramework.Empty);
            msBuildProject.TargetFrameworks.Should().Contain(TargetFramework.NetStandard2_0);
            msBuildProject.TargetFrameworks.Should().Contain(TargetFramework.NetCoreApp3_1);
        }

        [Fact]
        public async Task WhenProjectHasNoTargetFrameworkTheValueShouldBeEmpty()
        {
            using IFileSystem fileSystem = new MemoryFileSystem();

            UPath path = "/test.csproj";
            await using (var stream = fileSystem.CreateFile(path))
            {
                await stream.WriteAllTextAsync(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>

  </PropertyGroup>
</Project>");
            }

            var file = new FileEntry(fileSystem, path);

            var msBuildProject = await MsBuildProject.LoadFrom(file);

            msBuildProject.TargetFramework.Should().Be(TargetFramework.Empty);
            msBuildProject.TargetFrameworks.Should().BeEmpty();
        }

        [Fact]
        public async Task WhenProjectHasBothTargetFrameworkAnFrameworkTheValueShouldBeUsedFromFrameworks()
        {
            using IFileSystem fileSystem = new MemoryFileSystem();

            UPath path = "/test.csproj";
            await using (var stream = fileSystem.CreateFile(path))
            {
                await stream.WriteAllTextAsync(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <TargetFrameworks>netstandard2.0;netcoreapp3.1</TargetFrameworks>
  </PropertyGroup>
</Project>");
            }

            var file = new FileEntry(fileSystem, path);

            var msBuildProject = await MsBuildProject.LoadFrom(file);

            msBuildProject.TargetFramework.Should().Be(TargetFramework.Empty);
            msBuildProject.TargetFrameworks.Should().NotBeEmpty();
            msBuildProject.TargetFrameworks.Length.Should().Be(2);
        }

        [Fact]
        public void EqualsShouldReturnTrueForSameString()
        {
           var a= new TargetFramework("netstandard2.0");
           var b= new TargetFramework("netstandard2.0");

           a.Equals(b).Should().BeTrue();
        }[Fact]

        public void EqualsOperatorShouldReturnTrueForSameString()
        {
           var a= new TargetFramework("netstandard2.0");
           var b= new TargetFramework("netstandard2.0");

           (a == b).Should().BeTrue();
        }

        [Fact]
        public void EqualsShouldNotReturnTrueForDifferentString()
        {
           var a= new TargetFramework("netstandard2.0");
           var b= new TargetFramework("netstandard2.1");

           a.Equals(b).Should().BeFalse();
        }
    }
}