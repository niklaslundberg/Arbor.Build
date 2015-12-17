using System;
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
            //var appBaseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            //var dlls = appBaseDir.EnumerateFiles("*.dll");

            //foreach (var fileInfo in dlls)
            //{
            //    try
            //    {
            //        var loadedAssembly = Assembly.LoadFile(fileInfo.FullName);

            //        var assemblyName = loadedAssembly.GetName();

            //        try
            //        {
            //            var assembly = AppDomain.CurrentDomain.Load(assemblyName);

            //            var t = GetToolsFromAssembly(assembly, logger);

            //            tools.AddRange(t);
            //        }
            //        catch (ReflectionTypeLoadException)
            //        {
            //            //Ignore ReflectionTypeLoadException
            //        }
            //        catch (Exception ex)
            //        {
            //            logger.WriteWarning(string.Format("Could not load types from assembly {0}. {1}",
            //                fileInfo.FullName, ex));
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.WriteWarning(string.Format("Could not load assembly {0}. {1}", fileInfo.FullName, ex));
            //    }
            //}

            var tools = lifetimeScope.Resolve<IEnumerable<ITool>>().SafeToReadOnlyCollection();

            var prioritizedTools = tools
                .Select(tool =>
                {
                    var a =
                        tool.GetType()
                            .GetCustomAttributes()
                            .OfType<PriorityAttribute>()
                            .SingleOrDefault();

                    var priority = a != null ? a.Priority : int.MaxValue;

                    bool runAlways = a != null && a.RunAlways;

                    return new ToolWithPriority(tool, priority, runAlways);
                })
                .OrderBy(item => item.Priority)
                .ToList();

            return prioritizedTools;
        }

        static IEnumerable<ITool> GetToolsFromAssembly(Assembly assembly, ILogger logger)
        {
            var t = new List<ITool>();

            var types = assembly.GetTypes()
                .Where(type =>
                    !type.IsAbstract && type.IsClass &&
                    typeof (ITool).IsAssignableFrom(type));

            foreach (var type in types)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as ITool;

                    if (instance == null)
                    {
                        logger.WriteWarning(string.Format("Could not create instance from type {0}", type.Name));
                    }

                    t.Add(instance);
                }
                catch (Exception ex)
                {
                    logger.WriteWarning(string.Format("Could not create instance from type {0}. {1}", type.Name, ex));
                }
            }
            return t;
        }
    }
}