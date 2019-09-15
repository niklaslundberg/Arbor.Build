using System;
using Serilog;
using Serilog.Events;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class InMemoryLoggerHelper
    {
        public static ILogger CreateInMemoryLogger(Action<string, LogEventLevel> action) => new LoggerConfiguration().WriteTo.Sink(new InMemorySink(action)).CreateLogger();
    }
}
