using System;

namespace Arbor.Build.Core.Tools;

public class ToolResult(
    ToolWithPriority toolWithPriority,
    ToolResultType resultType,
    string? message = null,
    TimeSpan executionTime = default)
{
    public ToolWithPriority ToolWithPriority { get; } = toolWithPriority ?? throw new ArgumentNullException(nameof(toolWithPriority));

    public ToolResultType ResultType { get; } = resultType ?? throw new ArgumentNullException(nameof(resultType));

    public string? Message { get; } = message;

    public TimeSpan ExecutionTime { get; } = executionTime;
}