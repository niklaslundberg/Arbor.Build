using System;

namespace Arbor.X.Core.Tools
{
    public class ToolWithPriority
    {
        public ToolWithPriority(ITool tool, int priority,bool runAlways)
        {
            if (tool == null)
            {
                throw new ArgumentNullException("tool");
            }

            Tool = tool;
            Priority = priority;
            RunAlways = runAlways;
        }

        public ITool Tool { get; private set; }
        public int Priority { get; private set; }
        public bool RunAlways { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} (priority={1}, run always={2})", Tool, Priority, RunAlways);
        }
    }
}