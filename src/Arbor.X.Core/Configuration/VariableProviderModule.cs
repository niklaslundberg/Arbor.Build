using System.Linq;

using Arbor.X.Core.Assemblies;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Extensions;
using Arbor.X.Core.Tools.Environments;
using Autofac;
using JetBrains.Annotations;

namespace Arbor.X.Core
{
    [UsedImplicitly]
    public class VariableProviderModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assemblies = AssemblyFetcher.GetAssemblies().ToArray();

            builder.RegisterType<SourcePathVariableProvider>().AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(assemblies)
                .Where(type => type.IsConcretePublicClassImplementing<IVariableProvider>())
                .AsImplementedInterfaces();
        }
    }
}
