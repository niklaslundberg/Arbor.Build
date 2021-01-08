using System;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Bools;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.Logging
{
    public static class LoggerInitialization
    {
        public static ILogger InitializeLogging(string[] args, IEnvironmentVariables? environmentVariables = null)
        {
            environmentVariables ??= new DefaultEnvironmentVariables();

            LoggingLevelSwitch levelSwitch = LogLevelHelper.GetLevelSwitch(args, environmentVariables);

            string? seqUrl = args.FirstOrDefault(arg => arg.StartsWith("sequrl", StringComparison.OrdinalIgnoreCase))
                                ?.Split('=').LastOrDefault()
                            ?? environmentVariables.GetEnvironmentVariable("sequrl");

            string outputTemplate;

            if (environmentVariables.GetEnvironmentVariable(WellKnownVariables.ConsoleLogTimestampEnabled)
                .TryParseBool(out bool timestampsEnabled, true) && !timestampsEnabled)
            {
                outputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";
            }
            else if (environmentVariables.GetEnvironmentVariable(WellKnownVariables.IsRunningOnBuildAgent)
                .TryParseBool(out bool isRunningOnBuildAgent) && isRunningOnBuildAgent)
            {
                outputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";
            }
            else
            {
                outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            }

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: outputTemplate);

            if (!string.IsNullOrWhiteSpace(seqUrl) && Uri.TryCreate(seqUrl, UriKind.Absolute, out Uri? _))
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