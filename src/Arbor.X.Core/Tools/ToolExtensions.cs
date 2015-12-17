using System;

namespace Arbor.X.Core.Tools
{
    public static class ToolExtensions
    {
        public static string Name(this ITool tool)
        {
            if (tool == null)
            {
                throw new ArgumentNullException(nameof(tool));
            }
            return tool.GetType().Name;
        }
    }
}
