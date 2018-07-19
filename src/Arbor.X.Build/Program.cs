using System;
using System.Linq;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core;
using Arbor.X.Core.BuildVariables;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.X.Build
{
    internal class Program
    {
        private static BuildApplication _app;

        private static  async Task<int> Main(string[] args)
        {
            string logLevelArg = args?.FirstOrDefault(arg => arg.StartsWith(WellKnownVariables.LogLevel))?.Split('=')
                                     .LastOrDefault()
                                 ?? Environment.GetEnvironmentVariable(WellKnownVariables.LogLevel);

            var levelSwitch = new LoggingLevelSwitch();

            if (!string.IsNullOrWhiteSpace(logLevelArg))
            {
                if (Enum.TryParse(logLevelArg, true, out LogEventLevel logLevel))
                {
                    levelSwitch.MinimumLevel = logLevel;
                }
            }

            string seqUrl = args?.FirstOrDefault(arg => arg.StartsWith("sequrl"))?.Split('=').LastOrDefault()
                            ?? Environment.GetEnvironmentVariable("sequrl");

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Console();

            if (!string.IsNullOrWhiteSpace(seqUrl) && Uri.TryCreate(seqUrl, UriKind.Absolute, out Uri uri))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.Seq(seqUrl);
            }

            ILogger logger = loggerConfiguration
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();

            Log.Logger = logger;

            logger.Information("Using logging switch level {LogSwitchLevel}", levelSwitch);

            _app = new BuildApplication(logger);

            ExitCode exitCode = await _app.RunAsync(args);

            Log.CloseAndFlush();

            return exitCode.Result;
        }
    }
}
