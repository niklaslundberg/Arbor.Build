using System.Threading.Tasks;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Logging;
using Arbor.Processing;
using Serilog;

namespace Arbor.Build
{
    internal static class Program
    {
        private static BuildApplication _app;

        private static Task<int> Main(string[] args)
        {
            return RunAsync(args);
        }

        public static async Task<int> RunAsync(string[] args, IEnvironmentVariables? environmentVariables = default, ISpecialFolders specialFolders = default)
        {
            environmentVariables ??= new DefaultEnvironmentVariables();
            specialFolders ??= SpecialFolders.Default;

            ILogger logger = LoggerInitialization.InitializeLogging(args, environmentVariables);

            Log.Logger = logger;

            _app = new BuildApplication(logger, environmentVariables, specialFolders);

            ExitCode exitCode = await _app.RunAsync(args).ConfigureAwait(false);

            Log.CloseAndFlush();

            return exitCode.Code;
        }
    }
}
