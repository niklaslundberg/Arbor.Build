using System;
using Arbor.Processing.Core;
using Arbor.X.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Build
{
    internal class Program
    {
        private static BuildApplication _app;

        private static int Main(string[] args)
        {
            LogLevel logLevel = LogLevel.TryParse(Environment.GetEnvironmentVariable(WellKnownVariables.LogLevel));
            _app = new BuildApplication(new NLogLogger(logLevel));
            ExitCode exitCode = _app.RunAsync(args).Result;

            return exitCode.Result;
        }
    }
}