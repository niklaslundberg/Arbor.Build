using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Arbor.X.Core.Assemblies;
using Arbor.X.Core.GenericExtensions;
using Autofac;
using Autofac.Core;
using Autofac.Util;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.X.Core
{
    public static partial class BuildBootstrapper
    {
        public static Task<IContainer> StartAsync([NotNull] ILogger logger, string sourceDirectory = null)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var builder = new ContainerBuilder();

            Assembly[] assemblies = AssemblyFetcher.GetFilteredAssemblies().ToArray();

            builder.RegisterModule(new SerilogModule(logger));


            Type[] moduleTypes = assemblies.SelectMany(assembly => assembly.GetLoadableTypes().Where(type =>
                typeof(IModule).IsAssignableFrom(type) && !type.IsAbstract && type.HasSingleDefaultConstructor())).ToArray();

            foreach (Type moduleType in moduleTypes)
            {
                if (Activator.CreateInstance(moduleType) is IModule module)
                {
                    builder.RegisterModule(module);
                }
            }

            if (sourceDirectory != null)
            {
                builder.RegisterModule(new BuildVariableModule(sourceDirectory));
            }

            IContainer container = builder.Build();

            return container.AsCompletedTask();
        }
    }
}
