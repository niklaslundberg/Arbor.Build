using Serilog;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Serilog.Core;

namespace Arbor.X.Bootstrapper
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Logger logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            Task<ExitCode> startTask = new Bootstrapper(logger).StartAsync(args);

            ExitCode exitCode = startTask.Result;

            return exitCode.Result;
        }
    }
}
