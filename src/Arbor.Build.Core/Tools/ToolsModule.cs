using System.Linq;
using System.Reflection;
using Arbor.Build.Core.Assemblies;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.Tools.Git;
using Autofac;
using JetBrains.Annotations;
using Module = Autofac.Module;

namespace Arbor.Build.Core.Tools;

[UsedImplicitly]
public class ToolsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        Assembly[] assemblies = AssemblyFetcher.GetFilteredAssemblies().ToArray();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(type => type.IsConcretePublicClassImplementing<ITool>())
            .AsImplementedInterfaces();

        builder.RegisterType<GitHelper>();
    }
}