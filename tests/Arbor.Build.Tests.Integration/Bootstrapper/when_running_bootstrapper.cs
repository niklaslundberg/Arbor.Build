using System;
using System.IO;
using Arbor.Build.Core;
using Arbor.Build.Core.Bootstrapper;
using Arbor.Build.Core.IO;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Arbor.FS;
using Arbor.Processing;
using Machine.Specifications;
using Serilog.Core;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.Bootstrapper;

//[Ignore("Not complete")]
[Subject(typeof(AppBootstrapper))]
public class when_running_bootstrapper
{
    static AppBootstrapper _appBootstrapper;

    static BootstrapStartOptions startOptions;
    static ExitCode exitCode;
    static DirectoryEntry baseDirectory;
    static IFileSystem fs;

    Cleanup after = () =>
    {
        fs.Dispose();
    };

    Establish context = () =>
    {
        fs = new PhysicalFileSystem();

        baseDirectory = new DirectoryEntry(fs,
            UPath.Combine(VcsTestPathHelper.FindVcsRootPath().Path, "samples", "_NetStandardPackage"));

        startOptions = new BootstrapStartOptions(
            [],
            baseDirectory,
            true,
            "develop");
        _appBootstrapper = new AppBootstrapper(Logger.None, EnvironmentVariables.Empty, fs);
    };

    Because of = () => exitCode = _appBootstrapper.StartAsync(startOptions).Result;

    It should_return_success_exit_code = () => exitCode.IsSuccess.ShouldBeTrue();
}