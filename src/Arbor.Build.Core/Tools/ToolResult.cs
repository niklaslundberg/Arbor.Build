using System;

namespace Arbor.Build.Core.Tools;

public class ToolResult
{
    public ToolResult(
        ToolWithPriority toolWithPriority,
        ToolResultType resultType,
        string? message = null,
        TimeSpan executionTime = default)
    {
        ToolWithPriority = toolWithPriority ?? throw new ArgumentNullException(nameof(toolWithPriority));
        ResultType = resultType ?? throw new ArgumentNullException(nameof(resultType));
        Message = message;
        ExecutionTime = executionTime;
    }

    public ToolWithPriority ToolWithPriority { get; }

    public ToolResultType ResultType { get; }

    public string? Message { get; }

    public TimeSpan ExecutionTime { get; }
}