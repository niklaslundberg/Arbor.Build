using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Arbor.Build.Core.Assemblies;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.Logging;
using Autofac;
using Autofac.Core;
using Autofac.Util;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core
{
    public static class BuildBootstrapper
    {
        public static Task<IContainer> StartAsync([NotNull] ILogger logger, string? sourceDirectory = null)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var builder = new ContainerBuilder();

            RegisterLogging(logger, builder);

            RegisterAssemblyModules(builder);

            RegisterSourceRootConditionally(sourceDirectory, builder);

            IContainer container = builder.Build();

            return container.AsCompletedTask();
        }

        private static void RegisterSourceRootConditionally(string? sourceDirectory, ContainerBuilder builder)
        {
            if (!string.IsNullOrWhiteSpace(sourceDirectory))
            {
                builder.RegisterModule(new BuildVariableModule(sourceDirectory));
            }
        }

        private static void RegisterAssemblyModules(ContainerBuilder builder)
        {
            IModule[] modules = GetModulesFromAssemblies();

            foreach (IModule module in modules)
            {
                builder.RegisterModule(module);
            }
        }

        private static void RegisterLogging(ILogger logger, ContainerBuilder builder) => builder.RegisterModule(new SerilogModule(logger));

        private static IModule[] GetModulesFromAssemblies()
        {
            ImmutableHashSet<Assembly> assemblies = AssemblyFetcher.GetFilteredAssemblies();

            IModule[] modules = assemblies.SelectMany(assembly =>
                    assembly.GetLoadableTypes()
                        .Where(type =>
                            type.IsConcretePublicClassImplementing<IModule>()
                            && type.HasSingleDefaultConstructor()))
                .Select(type => Activator.CreateInstance(type) as IModule)
                .Where(module => module != null)
                .ToArray();

            return modules;
        }
    }
}
