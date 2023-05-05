using System.Linq;
using System.Reflection;
using Arbor.Build.Core.Assemblies;
using Arbor.Build.Core.GenericExtensions;
using Autofac;
using JetBrains.Annotations;
using Module = Autofac.Module;

namespace Arbor.Build.Core.BuildVariables;

[UsedImplicitly]
public class VariableProviderModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        Assembly[] assemblies = AssemblyFetcher.GetFilteredAssemblies().ToArray();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(type => type.IsConcretePublicClassImplementing<IVariableProvider>())
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}