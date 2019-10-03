using Serilog;
using Serilog.Events;

namespace Arbor.Build.Core.Logging
{
    public static class LoggerExtensions
    {
        public static void Log(this ILogger logger, string message, LogEventLevel level)
        {
            if (logger is null)
            {
                return;
            }

            switch (level)
            {
                case LogEventLevel.Verbose:
                    logger.Verbose("{Message}", message);
                    break;
                case LogEventLevel.Debug:
                    logger.Debug("{Message}", message);
                    break;
                case LogEventLevel.Information:
                    logger.Information("{Message}", message);
                    break;
                case LogEventLevel.Error:
                    logger.Error("{Message}", message);
                    break;
                case LogEventLevel.Fatal:
                    logger.Fatal("{Message}", message);
                    break;
            }
        }

        public static LogEventLevel MostVerboseLoggingCurrentLogLevel(this ILogger logger)
        {
            if (logger.IsEnabled(LogEventLevel.Verbose))
            {
                return LogEventLevel.Verbose;
            }

            if (logger.IsEnabled(LogEventLevel.Debug))
            {
                return LogEventLevel.Debug;
            }

            if (logger.IsEnabled(LogEventLevel.Information))
            {
                return LogEventLevel.Information;
            }

            if (logger.IsEnabled(LogEventLevel.Warning))
            {
                return LogEventLevel.Warning;
            }

            if (logger.IsEnabled(LogEventLevel.Error))
            {
                return LogEventLevel.Error;
            }

            return LogEventLevel.Fatal;
        }
    }
}
