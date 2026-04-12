using Xunit;
using Arbor.Build.Tests.Unit.Helpers;
using Zio;

namespace Arbor.Build.Tests.Unit.TestHelpers.Examples;

/// <summary>
/// Examples demonstrating how to use TestBuildContext for testing ITool implementations.
/// These are not actual tests but serve as documentation for the helper class.
/// </summary>
public class TestBuildContextExamples
{
    [Fact]
    public void Example_BasicContext()
    {
        // Simple context with in-memory file system
        var buildContext = TestBuildContext.Create()
            .WithSourceRoot("/root")
            .Build();

        Assert.NotNull(buildContext);
        Assert.True(buildContext.HasSourceRootSet);
    }

    [Fact]
    public void Example_ContextWithConfigurations()
    {
        // Context with Debug and Release configurations
        var buildContext = TestBuildContext.Create()
            .WithSourceRoot("/root")
            .WithConfigurations("Debug", "Release")
            .WithCurrentBuildConfiguration("Release")
            .Build();

        Assert.Contains("Debug", buildContext.Configurations);
        Assert.Contains("Release", buildContext.Configurations);
        Assert.NotNull(buildContext.CurrentBuildConfiguration);
    }

    [Fact]
    public void Example_ContextWithFiles()
    {
        // Context with sample solution and project files
        var buildContext = TestBuildContext.Create()
            .WithSourceRoot("/root")
            .WithFile("Sample.sln", "Microsoft Visual Studio Solution File...")
            .WithFile("src/Sample.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">")
            .WithDirectory("build")
            .Build();

        Assert.NotNull(buildContext);
        var fileSystem = buildContext.FileSystem;
        Assert.True(fileSystem.FileExists("/root/Sample.sln"));
        Assert.True(fileSystem.FileExists("/root/src/Sample.csproj"));
        Assert.True(fileSystem.DirectoryExists("/root/build"));
    }

    [Fact]
    public void Example_ContextForMSBuildNuGetRestorer()
    {
        // Realistic setup for testing MsBuildNuGetRestorer
        var buildContext = TestBuildContext.Create()
            .WithSourceRoot("/myproject")
            .WithFile("Solution.sln", "Microsoft Visual Studio Solution File, Format Version 12.00")
            .WithFile("src/MyProject.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\">")
            .WithConfigurations("Debug", "Release")
            .WithCurrentBuildConfiguration("Debug")
            .Build();

        // Now you can pass buildContext to tools that need it
        Assert.NotNull(buildContext.SourceRoot);
    }

    [Fact]
    public void Example_ContextWithComplexStructure()
    {
        // Complex project structure with multiple directories and files
        var buildContext = TestBuildContext.Create()
            .WithSourceRoot("/workspace")
            .WithDirectory("src")
            .WithDirectory("tests")
            .WithDirectory("samples")
            .WithFile("Directory.Build.props", "<Project><!-- Build properties --></Project>")
            .WithFile("src/Core.csproj", "<Project></Project>")
            .WithFile("src/Core/Program.cs", "namespace Core { public class Program {} }")
            .WithFile("tests/Core.Tests.csproj", "<Project></Project>")
            .WithFile("samples/Sample.csproj", "<Project></Project>")
            .WithConfigurations("Debug", "Release", "Custom")
            .Build();

        var fileSystem = buildContext.FileSystem;
        Assert.True(fileSystem.DirectoryExists("/workspace/src"));
        Assert.True(fileSystem.DirectoryExists("/workspace/tests"));
        Assert.True(fileSystem.DirectoryExists("/workspace/samples"));
        Assert.True(fileSystem.FileExists("/workspace/Directory.Build.props"));
        Assert.Equal(3, buildContext.Configurations.Count);
    }

    [Fact]
    public void Example_AccessFileSystemDirectly()
    {
        // Sometimes you need direct file system access
        var testContext = TestBuildContext.Create()
            .WithSourceRoot("/root");

        var fileSystem = testContext.GetFileSystem();
        
        // Create a file directly
        fileSystem.CreateDirectory("/root/temp");
        fileSystem.WriteAllText("/root/temp/test.txt", "content");
        
        // Then build the context
        var buildContext = testContext.Build();
        
        Assert.True(fileSystem.FileExists("/root/temp/test.txt"));
    }

    [Fact]
    public void Example_FluentAPIChaining()
    {
        // Demonstrate fluent API for readable test setup
        var buildContext = TestBuildContext
            .Create()
            .WithSourceRoot("/project")
            .WithFile("Solution.sln", "Microsoft Visual Studio Solution File")
            .WithFile("Directory.Build.props", "<Project />")
            .WithDirectory("src/Core")
            .WithDirectory("tests/Unit")
            .WithDirectory("build/Artifacts")
            .WithConfigurations("Debug", "Release")
            .WithCurrentBuildConfiguration("Debug")
            .Build();

        Assert.True(buildContext.HasSourceRootSet);
        Assert.NotNull(buildContext.CurrentBuildConfiguration);
    }
}
