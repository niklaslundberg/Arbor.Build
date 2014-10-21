using System;
using Arbor.X.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Build
{
    internal class Program
    {
        static BuildApplication _app;

        static int Main(string[] args)
        {
            var logLevel = LogLevel.TryParse(Environment.GetEnvironmentVariable(WellKnownVariables.LogLevel));
            _app = new BuildApplication(new NLogLogger(logLevel));
            ExitCode exitCode = _app.RunAsync(args).Result;

            return exitCode.Result;
        }
    }
}