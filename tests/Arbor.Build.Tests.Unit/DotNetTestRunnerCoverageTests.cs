using System.Collections.Generic;
using Arbor.Build.Core.Tools.Testing;
using Arbor.FS;
using Shouldly;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit;

public class DotNetTestRunnerCoverageTests
{
    [Fact]
    public void AddCodeCoverageArgumentsShouldAddCoverageArgumentsWhenEnabled()
    {
        using var fileSystem = new MemoryFileSystem();
        var arguments = new List<string>();
        var reportPath = "/repo/Artifacts/TestReports".ParseAsPath();

        DotNetTestRunner.AddCodeCoverageArguments(
            arguments,
            true,
            fileSystem,
            reportPath,
            "dotnet.MyProject.opencover.xml");

        arguments.ShouldContain("/p:CollectCoverage=true");
        arguments.ShouldContain("/p:CoverletOutputFormat=opencover");
        arguments.ShouldContain("/p:CoverletOutput=/repo/Artifacts/TestReports/Coverage/dotnet.MyProject.opencover.xml");
        fileSystem.DirectoryExists("/repo/Artifacts/TestReports/Coverage".ParseAsPath()).ShouldBeTrue();
    }

    [Fact]
    public void AddCodeCoverageArgumentsShouldNotAddCoverageArgumentsWhenDisabled()
    {
        using var fileSystem = new MemoryFileSystem();
        var arguments = new List<string>();
        var reportPath = "/repo/Artifacts/TestReports".ParseAsPath();

        DotNetTestRunner.AddCodeCoverageArguments(
            arguments,
            false,
            fileSystem,
            reportPath,
            "dotnet.MyProject.opencover.xml");

        arguments.ShouldBeEmpty();
        fileSystem.DirectoryExists("/repo/Artifacts/TestReports/Coverage".ParseAsPath()).ShouldBeFalse();
    }
}
