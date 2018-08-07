using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core;
using Serilog;

namespace Arbor.X.Build
{
    internal class Program
    {
        private static BuildApplication _app;

        private static async Task<int> Main(string[] args)
        {
            ILogger logger = LoggerInitialization.InitializeLogging(ref args);

            Log.Logger = logger;

            _app = new BuildApplication(logger);

            ExitCode exitCode = await _app.RunAsync(args);

            Log.CloseAndFlush();

            return exitCode.Result;
        }
    }
}
