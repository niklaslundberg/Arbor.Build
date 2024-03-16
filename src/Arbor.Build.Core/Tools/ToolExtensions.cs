using System;

namespace Arbor.Build.Core.Tools;

public static class ToolExtensions
{
    public static string Name(this ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        return tool.GetType().Name;
    }
}