using System;

namespace Arbor.Build.Core.Tools;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PriorityAttribute(int priority, bool runAlways = false) : Attribute
{
    public int Priority { get; } = priority;

    public bool RunAlways { get; } = runAlways;
}