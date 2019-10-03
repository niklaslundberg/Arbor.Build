using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.Logging;
using Arbor.Processing;
using Serilog;

namespace Arbor.Build
{
    internal static class Program
    {
        private static BuildApplication _app;

        private static async Task<int> Main(string[] args)
        {
            ILogger logger = LoggerInitialization.InitializeLogging(args);

            Log.Logger = logger;

            _app = new BuildApplication(logger);

            ExitCode exitCode = await _app.RunAsync(args).ConfigureAwait(false);

            Log.CloseAndFlush();

            return exitCode.Code;
        }
    }
}
