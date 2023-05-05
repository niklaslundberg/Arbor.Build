using Arbor.Build.Core.Tools.NuGet;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NuGet.Versioning;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit;

public class NuGetPackagerTests
{
    [Fact]
    public void WhenBranchNameIsDisabled()
    {
        var packageConfiguration = new NuGetPackageConfiguration("release", SemanticVersion.Parse("1.2.3"), new DirectoryEntry(new MemoryFileSystem(), UPath.Root), UPath.Root, branchName: "feature/abc", packageNameSuffix: ".Environment.Test");
        string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", packageConfiguration);
        Assert.Equal("MyPackage.Environment.Test", id);
    }

    [Fact]
    public void WhenBranchNameIsDisabledAndReleaseTrue()
    {
        var packageConfiguration = new NuGetPackageConfiguration("release", SemanticVersion.Parse("1.2.3"), new DirectoryEntry(new MemoryFileSystem(), UPath.Root), UPath.Root, branchName: "develop");
        string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", packageConfiguration);
        Assert.Equal("MyPackage", id);
    }

    [Fact]
    public void FeatureBranch()
    {
        var packageConfiguration = new NuGetPackageConfiguration("debug", SemanticVersion.Parse("1.2.3"), new DirectoryEntry(new MemoryFileSystem(), UPath.Root), UPath.Root, branchName: "feature/abc", branchNameEnabled: true);
        string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", packageConfiguration);
        Assert.Equal("MyPackage-abc", id);
    }

    [Theory]
    [InlineData("master")]
    [InlineData("main")]
    public void MainBranch(string branchName)
    {
        var packageConfiguration = new NuGetPackageConfiguration("release", SemanticVersion.Parse("1.2.3"), new DirectoryEntry(new MemoryFileSystem(), UPath.Root), UPath.Root, branchName: branchName, branchNameEnabled: true, packageNameSuffix: ".Environment.Production");
        string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", packageConfiguration);
        Assert.Equal("MyPackage.Environment.Production", id);
    }

    [Theory]
    [InlineData("develop")]
    [InlineData("dev")]
    public void DevBranch(string branchName)
    {
        var packageConfiguration = new NuGetPackageConfiguration("debug", SemanticVersion.Parse("1.2.3"), new DirectoryEntry(new MemoryFileSystem(), UPath.Root), UPath.Root, branchName: branchName, branchNameEnabled: true, packageNameSuffix: ".Environment.Production");
        string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", packageConfiguration);
        Assert.Equal("MyPackage.Environment.Production", id);
    }

    [Fact]
    public void FeatureBranchAndEnvironmentPackage()
    {
        var packageConfiguration = new NuGetPackageConfiguration("debug", SemanticVersion.Parse("1.2.3"), new DirectoryEntry(new MemoryFileSystem(), UPath.Root), UPath.Root, branchName: "feature/abc", branchNameEnabled: true, packageNameSuffix: ".Environment.Test");
        string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", packageConfiguration);
        Assert.Equal("MyPackage-abc.Environment.Test", id);
    }

    [InlineData("debug")]
    [InlineData("release")]
    [Theory]
    public void ReleaseBuild(string configuration)
    {
        var packageConfiguration = new NuGetPackageConfiguration(configuration, SemanticVersion.Parse("1.2.3"), new DirectoryEntry(new MemoryFileSystem(), UPath.Root), UPath.Root, branchName: "feature/abc", branchNameEnabled: true, isReleaseBuild: true);
        string id = NuGetPackageIdHelper.CreateNugetPackageId("MyPackage", packageConfiguration);
        Assert.Equal("MyPackage", id);
    }
}