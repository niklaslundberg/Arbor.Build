using System;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Core.Tools.NuGet;

public class InMemorySink(Action<string, LogEventLevel> action, LogEventLevel level = LogEventLevel.Information)
    : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        if (logEvent.Level >= level)
        {
            action.Invoke(logEvent.RenderMessage(), logEvent.Level);
        }
    }
}