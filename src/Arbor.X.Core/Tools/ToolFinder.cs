using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;
using Autofac;

namespace Arbor.X.Core.Tools
{
    public static class ToolFinder
    {
        public static IReadOnlyCollection<ToolWithPriority> GetTools(ILifetimeScope lifetimeScope, ILogger logger)
        {
            var tools = lifetimeScope.Resolve<IEnumerable<ITool>>().SafeToReadOnlyCollection();

            var prioritizedTools = tools
                .Select(tool =>
                {
                    var a =
                        tool.GetType()
                            .GetCustomAttributes()
                            .OfType<PriorityAttribute>()
                            .SingleOrDefault();

                    var priority = a?.Priority ?? int.MaxValue;

                    bool runAlways = a != null && a.RunAlways;

                    return new ToolWithPriority(tool, priority, runAlways);
                })
                .OrderBy(item => item.Priority)
                .ToList();

            return prioritizedTools;
        }
    }
}
