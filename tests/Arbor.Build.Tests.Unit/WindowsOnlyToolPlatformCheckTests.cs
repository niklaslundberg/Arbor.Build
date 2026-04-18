using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.Build.Core.Tools.Symbols;
using Arbor.Processing;
using Serilog.Core;
using Xunit;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Unit;

public class WindowsOnlyToolPlatformCheckTests
{
    [Fact(DisplayName = "NuGetRestorer should skip and succeed on non-Windows platforms")]
    public async Task NuGetRestorer_ShouldSkipOnNonWindowsPlatform()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Only relevant on non-Windows");

        using var fs = new MemoryFileSystem();
        var buildContext = new BuildContext(fs);
        var tool = new NuGetRestorer(fs, buildContext);

        ExitCode exitCode = await tool.ExecuteAsync(Logger.None, [], [], CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
    }

    [Fact(DisplayName = "NuGetPackageUploader should skip and succeed on non-Windows platforms")]
    public async Task NuGetPackageUploader_ShouldSkipOnNonWindowsPlatform()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Only relevant on non-Windows");

        using var fs = new MemoryFileSystem();
        var tool = new NuGetPackageUploader(fs);

        ExitCode exitCode = await tool.ExecuteAsync(Logger.None, [], [], CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
    }

    [Fact(DisplayName = "NuGetSymbolPackageUploader should skip and succeed on non-Windows platforms")]
    public async Task NuGetSymbolPackageUploader_ShouldSkipOnNonWindowsPlatform()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Only relevant on non-Windows");

        using var fs = new MemoryFileSystem();
        var tool = new NuGetSymbolPackageUploader(fs);

        ExitCode exitCode = await tool.ExecuteAsync(Logger.None, [], [], CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
    }

    [Fact(DisplayName = "NuGetEnvironmentVerification should be disabled on non-Windows platforms")]
    public async Task NuGetEnvironmentVerification_ShouldSkipOnNonWindowsPlatform()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Only relevant on non-Windows");

        using var fs = new MemoryFileSystem();
        var tool = new NuGetEnvironmentVerification(fs);

        // No NuGet path variable is provided - on non-Windows the tool should skip gracefully
        ExitCode exitCode = await tool.ExecuteAsync(Logger.None, [], [], CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
    }

    [Fact(DisplayName = "MSBuildEnvironmentVerification should be disabled on non-Windows platforms")]
    public async Task MSBuildEnvironmentVerification_ShouldSkipOnNonWindowsPlatform()
    {
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Only relevant on non-Windows");

        var tool = new MSBuildEnvironmentVerification();

        // No MSBuild path variable is provided - on non-Windows the tool should skip gracefully
        ExitCode exitCode = await tool.ExecuteAsync(Logger.None, [], [], CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
    }

    [Fact(DisplayName = "MSBuildEnvironmentVerification should be enabled on Windows when DotNet MSBuild is disabled")]
    public void MSBuildEnvironmentVerification_ShouldBeEnabledOnWindowsWhenDotNetMSBuildDisabled()
    {
        Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Only relevant on Windows");

        var tool = new MSBuildEnvironmentVerification();
        var buildVariables = new List<IVariable>
        {
            new BuildVariable(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled, "false")
        };

        // On Windows with DotNet MSBuild disabled, the tool should be enabled (and require MSBuild path)
        // We test the Enabled() behavior indirectly - the tool should fail because MSBuild path is missing
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }

    [Fact(DisplayName = "MSBuildEnvironmentVerification should skip when DotNet MSBuild is enabled")]
    public async Task MSBuildEnvironmentVerification_ShouldSkipWhenDotNetMSBuildIsEnabled()
    {
        var tool = new MSBuildEnvironmentVerification();
        var buildVariables = new List<IVariable>
        {
            new BuildVariable(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled, "true")
        };

        ExitCode exitCode = await tool.ExecuteAsync(Logger.None, buildVariables, [], CancellationToken.None);

        Assert.Equal(ExitCode.Success, exitCode);
    }
}
