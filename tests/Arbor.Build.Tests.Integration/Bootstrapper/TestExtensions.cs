using Serilog;
using Serilog.Configuration;
using Xunit;

namespace Arbor.Build.Tests.Integration.Bootstrapper
{
    public static class TestExtensions
    {
        public static LoggerConfiguration Test(this LoggerSinkConfiguration loggerSinkConfiguration,
            ITestOutputHelper testOutputHelper) =>
            loggerSinkConfiguration.Sink(new DelegatingSink(@event => testOutputHelper.WriteLine(@event.RenderMessage())));
    }
}