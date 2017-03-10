using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arbor.Defensive.Collections;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;
using Autofac;

namespace Arbor.X.Core.Tools
{
    public static class ToolFinder
    {
        public static IReadOnlyCollection<ToolWithPriority> GetTools(ILifetimeScope lifetimeScope, ILogger logger)
        {
            IReadOnlyCollection<ITool> tools = lifetimeScope.Resolve<IEnumerable<ITool>>().SafeToReadOnlyCollection();

            List<ToolWithPriority> prioritizedTools = tools
                .Select(tool =>
                {
                    var priorityAttribute =
                        tool.GetType()
                            .GetCustomAttributes()
                            .OfType<PriorityAttribute>()
                            .SingleOrDefault();

                    var priority = priorityAttribute?.Priority ?? int.MaxValue;

                    bool runAlways = priorityAttribute != null && priorityAttribute.RunAlways;

                    return new ToolWithPriority(tool, priority, runAlways);
                })
                .OrderBy(item => item.Priority)
                .ToList();

            return prioritizedTools;
        }
    }
}
