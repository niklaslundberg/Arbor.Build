using System;
using System.Threading.Tasks;
using Arbor.X.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

namespace Arbor.X.Bootstrapper
{
    internal class Program
    {
        static int Main(string[] args)
        {
            LogLevel logLevel = LogLevel.TryParse(Environment.GetEnvironmentVariable(WellKnownVariables.LogLevel));

            Task<ExitCode> startTask = new Bootstrapper(logLevel).StartAsync(args);

            ExitCode exitCode = startTask.Result;

            return exitCode.Result;
        }
    }
}