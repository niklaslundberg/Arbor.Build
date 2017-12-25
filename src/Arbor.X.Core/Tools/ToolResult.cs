using System;

namespace Arbor.X.Core.Tools
{
    public class ToolResult
    {
        public ToolResult(
            ToolWithPriority toolWithPriority,
            ToolResultType resultType,
            string message = null,
            TimeSpan executionTime = default)
        {
            if (toolWithPriority == null)
            {
                throw new ArgumentNullException(nameof(toolWithPriority));
            }

            if (resultType == null)
            {
                throw new ArgumentNullException(nameof(resultType));
            }

            ToolWithPriority = toolWithPriority;
            ResultType = resultType;
            Message = message;
            ExecutionTime = executionTime;
        }

        public ToolWithPriority ToolWithPriority { get; }

        public ToolResultType ResultType { get; }

        public string Message { get; }

        public TimeSpan ExecutionTime { get; }
    }
}
