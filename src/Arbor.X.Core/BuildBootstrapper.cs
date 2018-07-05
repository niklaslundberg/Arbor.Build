using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Arbor.X.Core.Assemblies;
using Arbor.X.Core.GenericExtensions;
using Autofac;
using Autofac.Core;
using Autofac.Util;
using Serilog;
using Module = Autofac.Module;

namespace Arbor.X.Core
{
    public class BuildBootstrapper
    {
        public static Task<IContainer> StartAsync(ILogger logger)
        {
            var builder = new ContainerBuilder();

            Assembly[] assemblies = AssemblyFetcher.GetAssemblies().ToArray();

            builder.RegisterModule(new SerilogModule(logger));

            Type[] moduleTypes = assemblies.SelectMany(a => a.GetLoadableTypes().Where(t =>
                typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructors().Length == 1 &&
                t.GetConstructor(Array.Empty<Type>())?.GetParameters().Length == 0)).ToArray();

            foreach (Type moduleType in moduleTypes)
            {
                if (Activator.CreateInstance(moduleType) is IModule module)
                {
                    builder.RegisterModule(module);
                }
            }

            IContainer container = builder.Build();

            return container.AsCompletedTask();
        }

        public class SerilogModule : Module
        {
            readonly ILogger logger;

            public SerilogModule(ILogger logger)
            {
                this.logger = logger;
            }

            protected override void Load(ContainerBuilder builder)
            {
                builder.RegisterInstance(logger).AsImplementedInterfaces().SingleInstance();
            }
        }
    }
}
