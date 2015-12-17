using System.Linq;

using Arbor.X.Core.Assemblies;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Tools;

using Autofac;

using JetBrains.Annotations;

namespace Arbor.X.Core.Configuration.AutofacModules
{
    [UsedImplicitly]
    public class ToolsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assemblies = AssemblyFetcher.GetAssemblies().ToArray();

            builder.RegisterAssemblyTypes(assemblies)
                .Where(type => type.IsConcretePublicClassImplementing<ITool>())
                .AsImplementedInterfaces();
        }
    }
}
