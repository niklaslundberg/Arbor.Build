using System;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Logging;
using Arbor.Processing;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Bootstrapper
{
    internal static class Program
    {
        private static Task<int> Main(string[] args)
        {
            return RunAsync(args);
        }

        public static async Task<int> RunAsync(string[] args, IEnvironmentVariables? environmentVariables = default)
        {
            environmentVariables ??= new DefaultEnvironmentVariables();

            Logger logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var bootstrapper = new Core.Bootstrapper.AppBootstrapper(logger, environmentVariables);

            ExitCode exitCode = await bootstrapper.StartAsync(args).ConfigureAwait(false);

            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return exitCode.Code;
        }
    }
}
