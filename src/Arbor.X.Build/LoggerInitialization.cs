using System;
using System.Linq;
using Arbor.X.Core.BuildVariables;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.X.Build
{
    internal static class LoggerInitialization
    {
        internal static ILogger InitializeLogging(ref string[] args)
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

            logger.Information("Using logging switch level {LogSwitchLevel}", levelSwitch);

            return logger;
        }
    }
}
