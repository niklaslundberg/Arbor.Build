using System;
using Serilog;

namespace Arbor.X.Core.Tools.NuGet
{
    public static class InMemoryLoggerHelper
    {
        public static ILogger CreateInMemoryLogger(Action<string> action)
        {
            return new LoggerConfiguration().WriteTo.Sink(new InMemorySink(action)).CreateLogger();
        }
    }
}