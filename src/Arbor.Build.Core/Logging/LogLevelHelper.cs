using System;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Core.Logging;

public static class LogLevelHelper
{
    public static LoggingLevelSwitch GetLevelSwitch(string[]? args, IEnvironmentVariables environmentVariables)
    {
        string? logLevelArg = args?.FirstOrDefault(arg => arg.StartsWith(WellKnownVariables.LogLevel,
                                      StringComparison.OrdinalIgnoreCase))
                                  ?.Split('=')
                                  .LastOrDefault()
                              ?? environmentVariables.GetEnvironmentVariable(WellKnownVariables.LogLevel);

        var levelSwitch = new LoggingLevelSwitch();

        if (!string.IsNullOrWhiteSpace(logLevelArg) && Enum.TryParse(logLevelArg, true, out LogEventLevel logLevel))
        {
            levelSwitch.MinimumLevel = logLevel;
        }

        return levelSwitch;
    }
}