using System;

namespace Arbor.X.Core.Tools
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PriorityAttribute : Attribute
    {
        public PriorityAttribute(int priority, bool runAlways = false)
        {
            Priority = priority;
            RunAlways = runAlways;
        }

        public int Priority { get; private set; }
        public bool RunAlways { get; private set; }
    }
}