using System;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Boolean;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.Logging
{
    public static class LoggerInitialization
    {
        public static ILogger InitializeLogging(string[] args)
        {
            LoggingLevelSwitch levelSwitch = LogLevelHelper.GetLevelSwitch(args);

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
