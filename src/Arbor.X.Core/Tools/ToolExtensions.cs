using System;

using Arbor.X.Core.Tools;

namespace Arbor.X.Core.Extensions
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
