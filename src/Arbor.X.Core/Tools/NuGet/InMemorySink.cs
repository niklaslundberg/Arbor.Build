using System;
using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class InMemorySink : ILogEventSink
    {
        private readonly Action<string, LogEventLevel> _action;

        private readonly LogEventLevel _level;

        public InMemorySink(Action<string, LogEventLevel> action, LogEventLevel level = LogEventLevel.Information)
        {
            _action = action;
            _level = level;
        }

        public void Emit([NotNull] LogEvent logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            if (logEvent.Level >= _level)
            {
                _action.Invoke(logEvent.RenderMessage(), logEvent.Level);
            }
        }
    }
}
