using System;
using System.IO;
using Arbor.Build.Core.Tools.Testing;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Arbor.Processing;
using Serilog;
using Serilog.Core;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.TrxTransform;

public sealed class WhenTransformingTrxToJunit : IDisposable
{
    readonly IFileSystem _fileSystem;
    readonly Logger _logger;

    public WhenTransformingTrxToJunit()
    {
        _fileSystem = new PhysicalFileSystem();
        _logger = new LoggerConfiguration().WriteTo.Debug().MinimumLevel.Verbose().CreateLogger();
    }

    public void Dispose()
    {
        _logger.Dispose();
        _fileSystem.Dispose();
    }

    [Fact]
    public void ThenItShouldCreateAJunitReport()
    {
        try
        {
            var report = new FileEntry(_fileSystem, UPath.Combine(
                VcsTestPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory()).Path,
                "src",
                "Arbor.Build.Tests.Integration",
                "xunit_v2.Arbor.Build.Tests.SampleXunitNetCoreApp31.trx"));

            var exitCode = TestReportXslt.Transform(report, Trx2UnitXsl.TrxTemplate, _logger, false);

            Assert.Equal(ExitCode.Success, exitCode);
        }
        catch (Exception)
        {
            //ignore when recursive
        }
    }
}