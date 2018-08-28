using System;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Boolean;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build
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

            string seqUrl = args?.FirstOrDefault(arg => arg.StartsWith("sequrl", StringComparison.OrdinalIgnoreCase))?.Split('=').LastOrDefault()
                            ?? Environment.GetEnvironmentVariable("sequrl");

            string outputTemplate;

            if (Environment.GetEnvironmentVariable(WellKnownVariables.ConsoleLogTimestampEnabled).TryParseBool(out bool timestampsEnabled, defaultValue: true) && !timestampsEnabled)
            {
                outputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";
            }
            else if (Environment.GetEnvironmentVariable(WellKnownVariables.IsRunningOnBuildAgent).TryParseBool(out bool isRunningOnBuildAgent) && isRunningOnBuildAgent)
            {
                outputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";
            }
            else
            {
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            }

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: outputTemplate);

            if (!string.IsNullOrWhiteSpace(seqUrl) && Uri.TryCreate(seqUrl, UriKind.Absolute, out Uri uri))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.Seq(seqUrl);
            }

            ILogger logger = loggerConfiguration
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();

            logger.Information("Using logging switch level {LogSwitchLevel}", levelSwitch.MinimumLevel);

            return logger;
        }
    }
}
