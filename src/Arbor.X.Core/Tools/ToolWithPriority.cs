using System;

namespace Arbor.X.Core.Tools
{
    public class ToolWithPriority
    {
        public ToolWithPriority(ITool tool, int priority, bool runAlways)
        {
            if (tool == null)
            {
                throw new ArgumentNullException(nameof(tool));
            }

            Tool = tool;
            Priority = priority;
            RunAlways = runAlways;
        }

        public ITool Tool { get; }

        public int Priority { get; }

        public bool RunAlways { get; }

        public override string ToString()
        {
            return $"{Tool} (priority={Priority}, run always={RunAlways})";
        }
    }
}
