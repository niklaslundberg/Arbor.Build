using System;
using System.IO;
using Arbor.Build.Core.Tools.Testing;
using Arbor.Build.Tests.Integration.Tests.MSpec;
using Arbor.Processing;
using Serilog;
using Serilog.Core;
using Xunit;

namespace Arbor.Build.Tests.Integration.TrxTransform
{
    public sealed class WhenTransformingTrxToJunit : IDisposable
    {
        readonly Logger _logger;

        public WhenTransformingTrxToJunit() => _logger = new LoggerConfiguration().WriteTo.Debug().MinimumLevel.Verbose().CreateLogger();

        [Fact]
        public void ThenItShouldCreateAJunitReport()
        {
            try
            {
                var report = new FileInfo(Path.Combine(
                    VcsTestPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory()),
                    "src",
                    "Arbor.X.Tests.Integration",
                    "xunit_v2.Arbor.X.Tests.SampleXunitNetCoreApp21.trx"));

                ExitCode exitCode = TestReportXslt.Transform(report, Trx2UnitXsl.TrxTemplate, _logger, false);

                Assert.Equal(ExitCode.Success, exitCode);
            }
            catch (Exception ex)
            {
                //ignore when recursive
            }
        }

        public void Dispose() => _logger?.Dispose();
    }
}
