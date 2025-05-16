using System.IO;
using System.Threading.Tasks;
using Arbor.Build.Core.Bootstrapper;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Machine.Specifications;
using NUnit.Framework.Internal;
using Serilog;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.Bootstrapper;

public class BootstrapperTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task RunningBootstrapper()
    {
        using var fs = new PhysicalFileSystem();

        using var tempDirectory = TempDirectory.Create(fs);

        await using var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Debug()
            .WriteTo.Test(testOutputHelper)
            .MinimumLevel.Debug()
            .CreateLogger();

        var sourcePath = new DirectoryEntry(fs,
            UPath.Combine(VcsTestPathHelper.FindVcsRootPath().Path, "samples", "_NetStandardPackage"));

        using var baseDirectory = TempDirectory.Create(fs);

        await using (var _ = baseDirectory.Directory.FileSystem.OpenFile(baseDirectory.Directory.Path / ".gitattributes",
                         FileMode.Create, FileAccess.Write))
        {
        }

        await DirectoryCopy.CopyAsync(sourcePath, baseDirectory.Directory, pathLookupSpecificationOption: new PathLookupSpecification(), cancellationToken: TestContext.Current.CancellationToken);

        var startOptions = new BootstrapStartOptions(
            [],
            baseDirectory.Directory,
            true,
            "develop",
            tempDirectory: tempDirectory.Directory);

        var variables = new EnvironmentVariables();
        variables.SetEnvironmentVariable(WellKnownVariables.DirectoryCloneEnabled, "true");
        var appBootstrapper = new AppBootstrapper(logger, variables, fs);

        var exitCode = await appBootstrapper.StartAsync(startOptions, TestContext.Current.CancellationToken);

        exitCode.Code.ShouldEqual(0);
    }
}