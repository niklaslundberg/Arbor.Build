using System;

namespace Arbor.Build.Core.Tools;

public class ToolWithPriority(ITool tool, int priority, bool runAlways)
{
    public ITool Tool { get; } = tool ?? throw new ArgumentNullException(nameof(tool));

    public int Priority { get; } = priority;

    public bool RunAlways { get; } = runAlways;

    public override string ToString() => $"{Tool} (priority={Priority}, run always={RunAlways})";
}