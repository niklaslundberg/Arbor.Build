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
            var report = new FileInfo(Path.Combine(VcsTestPathHelper.FindVcsRootPath(),
                "src",
                "Arbor.Build.Tests.Integration",
                "xunit_v2.Arbor.X.Tests.SampleXunitNetCoreApp21.trx"));

            ExitCode exitCode = TestReportXslt.Transform(report, XUnitV2JUnitXsl.TrxTemplate, _logger, false);

            Assert.Equal(ExitCode.Success, exitCode);
        }

        public void Dispose() => _logger?.Dispose();
    }
}
