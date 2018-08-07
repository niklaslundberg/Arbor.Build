using System;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools
{
    public class ToolResult
    {
        public ToolResult(
            [NotNull] ToolWithPriority toolWithPriority,
            [NotNull] ToolResultType resultType,
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

            ToolWithPriority = toolWithPriority ?? throw new ArgumentNullException(nameof(toolWithPriority));
            ResultType = resultType ?? throw new ArgumentNullException(nameof(resultType));
            Message = message;
            ExecutionTime = executionTime;
        }

        public ToolWithPriority ToolWithPriority { get; }

        public ToolResultType ResultType { get; }

        public string Message { get; }

        public TimeSpan ExecutionTime { get; }
    }
}
