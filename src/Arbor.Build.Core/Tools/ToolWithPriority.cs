using System;

namespace Arbor.Build.Core.Tools;

public class ToolWithPriority
{
    public ToolWithPriority(ITool tool, int priority, bool runAlways)
    {
        Tool = tool ?? throw new ArgumentNullException(nameof(tool));
        Priority = priority;
        RunAlways = runAlways;
    }

    public ITool Tool { get; }

    public int Priority { get; }

    public bool RunAlways { get; }

    public override string ToString() => $"{Tool} (priority={Priority}, run always={RunAlways})";
}