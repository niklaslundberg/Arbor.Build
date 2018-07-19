using System;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.X.Core.Tools.NuGet
{
    public class InMemorySink : ILogEventSink
    {
        private readonly Action<string> action;

        public InMemorySink(Action<string> action)
        {
            this.action = action;
        }

        public void Emit(LogEvent logEvent)
        {
            action.Invoke(logEvent.RenderMessage());
        }
    }
}