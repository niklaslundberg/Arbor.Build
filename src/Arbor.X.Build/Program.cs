using Serilog;
using Arbor.Processing.Core;
using Arbor.X.Core;

namespace Arbor.X.Build
{
    internal class Program
    {
        private static BuildApplication _app;

        private static int Main(string[] args)
        {
            ILogger logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            _app = new BuildApplication(logger);
            ExitCode exitCode = _app.RunAsync(args).Result;

            return exitCode.Result;
        }
    }
}
