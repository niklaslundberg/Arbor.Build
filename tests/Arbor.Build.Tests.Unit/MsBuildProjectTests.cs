using System.Text;
using System.Threading.Tasks;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.FS;
using Shouldly;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit;

public class MsBuildProjectTests
{
    [Fact]
    public async Task WhenProjectHasSingleTargetFrameworkTheValueShouldNotBeNull()
    {
        using IFileSystem fileSystem = new MemoryFileSystem();

        UPath path = "/test.csproj";
        await using (var stream = fileSystem.CreateFile(path))
        {
            await stream.WriteAllTextAsync("""
                                           <Project Sdk="Microsoft.NET.Sdk">
                                             <PropertyGroup>
                                               <TargetFramework>netstandard2.0</TargetFramework>
                                             </PropertyGroup>
                                           </Project>
                                           """, Encoding.UTF8, cancellationToken: TestContext.Current.CancellationToken);
        }

        var file = new FileEntry(fileSystem, path);

        var msBuildProject = await MsBuildProject.LoadFrom(file, TestContext.Current.CancellationToken);

        msBuildProject.TargetFramework.ShouldBe(TargetFramework.NetStandard2_0);
        msBuildProject.TargetFrameworks.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task WhenProjectHasMultipleTargetFrameworksTheTargetShouldNotBeEmpty()
    {
        using IFileSystem fileSystem = new MemoryFileSystem();

        UPath path = "/test.csproj";
        await using (var stream = fileSystem.CreateFile(path))
        {
            await stream.WriteAllTextAsync("""
                                           <Project Sdk="Microsoft.NET.Sdk">
                                             <PropertyGroup>
                                               <TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
                                             </PropertyGroup>
                                           </Project>
                                           """, Encoding.UTF8, cancellationToken: TestContext.Current.CancellationToken);
        }

        var file = new FileEntry(fileSystem, path);

        var msBuildProject = await MsBuildProject.LoadFrom(file, TestContext.Current.CancellationToken);

        msBuildProject.TargetFramework.ShouldBe(TargetFramework.Empty);
        msBuildProject.TargetFrameworks.ShouldContain(TargetFramework.NetStandard2_0);
        msBuildProject.TargetFrameworks.ShouldContain(TargetFramework.Net8_0);
    }

    [Fact]
    public async Task WhenProjectHasNoTargetFrameworkTheValueShouldBeEmpty()
    {
        using IFileSystem fileSystem = new MemoryFileSystem();

        UPath path = "/test.csproj";
        await using (var stream = fileSystem.CreateFile(path))
        {
            await stream.WriteAllTextAsync("""
                                           <Project Sdk="Microsoft.NET.Sdk">
                                             <PropertyGroup>

                                             </PropertyGroup>
                                           </Project>
                                           """, Encoding.UTF8, cancellationToken: TestContext.Current.CancellationToken);
        }

        var file = new FileEntry(fileSystem, path);

        var msBuildProject = await MsBuildProject.LoadFrom(file, TestContext.Current.CancellationToken);

        msBuildProject.TargetFramework.ShouldBe(TargetFramework.Empty);
        msBuildProject.TargetFrameworks.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenProjectHasBothTargetFrameworkAnFrameworkTheValueShouldBeUsedFromFrameworks()
    {
        using IFileSystem fileSystem = new MemoryFileSystem();

        UPath path = "/test.csproj";
        await using (var stream = fileSystem.CreateFile(path))
        {
            await stream.WriteAllTextAsync("""
                                           <Project Sdk="Microsoft.NET.Sdk">
                                             <PropertyGroup>
                                               <TargetFramework>netstandard2.0</TargetFramework>
                                               <TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
                                             </PropertyGroup>
                                           </Project>
                                           """, Encoding.UTF8, cancellationToken: TestContext.Current.CancellationToken);
        }

        var file = new FileEntry(fileSystem, path);

        var msBuildProject = await MsBuildProject.LoadFrom(file, TestContext.Current.CancellationToken);

        msBuildProject.TargetFramework.ShouldBe(TargetFramework.Empty);
        msBuildProject.TargetFrameworks.ShouldNotBeEmpty();
        msBuildProject.TargetFrameworks.Length.ShouldBe(2);
    }

    [Fact]
    public void EqualsShouldReturnTrueForSameString()
    {
        var a = new TargetFramework("netstandard2.0");
        var b = new TargetFramework("netstandard2.0");

        a.Equals(b).ShouldBeTrue();
    }
    [Fact]

    public void EqualsOperatorShouldReturnTrueForSameString()
    {
        var a = new TargetFramework("netstandard2.0");
        var b = new TargetFramework("netstandard2.0");

        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void EqualsShouldNotReturnTrueForDifferentString()
    {
        var a = new TargetFramework("netstandard2.0");
        var b = new TargetFramework("netstandard2.1");

        a.Equals(b).ShouldBeFalse();
    }
}