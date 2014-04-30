using System.Threading.Tasks;
using Arbor.X.Core;

namespace Arbor.X.Bootstrapper
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Task<ExitCode> startTask = new Bootstrapper().StartAsync(args);

            ExitCode exitCode = startTask.Result;

            return exitCode.Result;
        }
    }
}