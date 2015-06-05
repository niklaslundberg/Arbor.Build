using System.Linq;
using System.Threading.Tasks;
using Autofac;

namespace Arbor.X.Core
{
    public class BuildBootstrapper
    {
        public static Task<IContainer> StartAsync()
        {
            ContainerBuilder builder = new ContainerBuilder();

            var assemblies = AssemblyExtensions.GetAssemblies().ToArray();

            builder.RegisterAssemblyModules(assemblies);

            IContainer container = builder.Build();

            return container.AsCompletedTask();
        }
    }
}