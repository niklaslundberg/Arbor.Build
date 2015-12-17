using System.Linq;

using Arbor.X.Core.Assemblies;
using Arbor.X.Core.Extensions;
using Arbor.X.Core.Tools;
using Autofac;
using JetBrains.Annotations;

namespace Arbor.X.Core
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
