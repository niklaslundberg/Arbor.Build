﻿using System.Threading.Tasks;
using Arbor.Build.Core.Bootstrapper;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Machine.Specifications;
using Serilog;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.Bootstrapper;

public class BootstrapperTests
{
    [Fact]
    public async Task RunningBootstrapper()
    {
        using var fs = new PhysicalFileSystem();

        using var tempDirectory = TempDirectory.Create(fs);

        await using var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Debug()
            .MinimumLevel.Debug()
            .CreateLogger();

        var sourcePath = new DirectoryEntry(fs,
            UPath.Combine(VcsTestPathHelper.FindVcsRootPath().Path, "samples", "_NetStandardPackage"));

        using var baseDirectory = TempDirectory.Create(fs);

        await DirectoryCopy.CopyAsync(sourcePath, baseDirectory.Directory, pathLookupSpecificationOption: new PathLookupSpecification());

        var startOptions = new BootstrapStartOptions(
            [],
            baseDirectory.Directory,
            true,
            "develop",
            tempDirectory: tempDirectory.Directory);
        var variables = new EnvironmentVariables();
        variables.SetEnvironmentVariable(WellKnownVariables.DirectoryCloneEnabled, "true");
        var appBootstrapper = new AppBootstrapper(logger, variables, fs);

        var exitCode = await appBootstrapper.StartAsync(startOptions);

        exitCode.Code.ShouldEqual(0);
    }
}