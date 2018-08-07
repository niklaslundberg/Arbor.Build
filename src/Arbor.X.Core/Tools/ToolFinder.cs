using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Autofac;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core.Tools
{
    public static class ToolFinder
    {
        public static ImmutableArray<ToolWithPriority> GetTools([NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ILogger logger)
        {
            if (lifetimeScope == null)
            {
                throw new ArgumentNullException(nameof(lifetimeScope));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var tools = lifetimeScope.Resolve<IReadOnlyCollection<ITool>>();

            ImmutableArray<ToolWithPriority> prioritizedTools = tools
                .Select(tool =>
                {
                    PriorityAttribute priorityAttribute =
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
}
