using System;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Tests.Integration;

public class DelegatingSink(Action<LogEvent> write) : ILogEventSink
{
    readonly Action<LogEvent> _write = write ?? throw new ArgumentNullException(nameof(write));

    public void Emit(LogEvent logEvent) => _write(logEvent);
}