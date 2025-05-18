using System.Linq;
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
        var assemblies = AssemblyFetcher.FilteredAssemblies;

        builder.RegisterAssemblyTypes(assemblies.ToArray())
            .Where(type => type.IsConcretePublicClassImplementing<IVariableProvider>())
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}