using Arbor.X.Core;
using Arbor.X.Core.Logging;

namespace Arbor.X.Build
{
    internal class Program
    {
        static BuildApplication _app;

        static int Main(string[] args)
        {
            _app = new BuildApplication(new ConsoleLogger());
            ExitCode exitCode = _app.RunAsync().Result;

            return exitCode.Result;
        }
    }
}