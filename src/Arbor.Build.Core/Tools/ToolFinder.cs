using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Autofac;
using Serilog;

namespace Arbor.Build.Core.Tools;

public static class ToolFinder
{
    public static ImmutableArray<ToolWithPriority> GetTools(
        ILifetimeScope lifetimeScope,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);

        ArgumentNullException.ThrowIfNull(logger);

        var tools = lifetimeScope.Resolve<IReadOnlyCollection<ITool>>();

        var prioritizedTools = tools
            .Select(tool =>
            {
                PriorityAttribute? priorityAttribute =
                    tool.GetType()
                        .GetCustomAttributes()
                        .OfType<PriorityAttribute>()
                        .SingleOrDefault();

                int priority = priorityAttribute?.Priority ?? int.MaxValue;

                bool runAlways = priorityAttribute != null && priorityAttribute.RunAlways;

                return new ToolWithPriority(tool, priority, runAlways);
            })
            .OrderBy(item => item.Priority)
            .ToImmutableArray();

        logger.Verbose("Found {Count} prioritized tools", prioritizedTools.Length);

        return prioritizedTools;
    }
}