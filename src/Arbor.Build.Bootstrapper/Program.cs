using System;
using System.Threading.Tasks;
using Arbor.Processing;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Bootstrapper
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Logger logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var bootstrapper = new Core.Bootstrapper.AppBootstrapper(logger);

            ExitCode exitCode = await bootstrapper.StartAsync(args).ConfigureAwait(false);

            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return exitCode.Code;
        }
    }
}
