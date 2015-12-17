using System;

namespace Arbor.X.Core.Tools
{
    public class ToolResult
    {
        readonly TimeSpan _executionTime;
        readonly string _message;
        readonly ToolResultType _resultType;
        readonly ToolWithPriority _toolWithPriority;

        public ToolResult(ToolWithPriority toolWithPriority, ToolResultType resultType, string message = null,
            TimeSpan executionTime = default(TimeSpan))
        {
            if (toolWithPriority == null)
            {
                throw new ArgumentNullException(nameof(toolWithPriority));
            }

            if (resultType == null)
            {
                throw new ArgumentNullException(nameof(resultType));
            }

            _toolWithPriority = toolWithPriority;
            _resultType = resultType;
            _message = message;
            _executionTime = executionTime;
        }

        public ToolWithPriority ToolWithPriority => _toolWithPriority;

        public ToolResultType ResultType => _resultType;

        public string Message => _message;

        public TimeSpan ExecutionTime => _executionTime;
    }
}