using System;

namespace Arbor.Build.Core.Tools;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PriorityAttribute : Attribute
{
    public PriorityAttribute(int priority, bool runAlways = false)
    {
        Priority = priority;
        RunAlways = runAlways;
    }

    public int Priority { get; }

    public bool RunAlways { get; }
}