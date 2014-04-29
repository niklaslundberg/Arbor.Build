using System;
using Arbor.X.Core.Tools;

namespace Arbor.X.Core
{
    public static class ToolExtensions
    {
        public static string Name(this ITool tool)
        {
            if (tool == null)
            {
                throw new ArgumentNullException("tool");
            }
            return tool.GetType().Name;
        }
    }
}