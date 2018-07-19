using System;
using Serilog;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Serilog.Core;

namespace Arbor.X.Bootstrapper
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            Logger logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var bootstrapper = new Core.Bootstrapper.Bootstrapper(logger);

            ExitCode exitCode = await bootstrapper.StartAsync(args);

            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return exitCode.Result;
        }
    }
}
