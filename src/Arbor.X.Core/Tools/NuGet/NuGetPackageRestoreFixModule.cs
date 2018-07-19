using System.Linq;
using System.Reflection;
using Arbor.X.Core.Assemblies;
using Arbor.X.Core.GenericExtensions;
using Autofac;
using JetBrains.Annotations;
using Module = Autofac.Module;

namespace Arbor.X.Core.Tools.NuGet
{
    [UsedImplicitly]
    public class NuGetPackageRestoreFixModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Assembly[] assemblies = AssemblyFetcher.GetFilteredAssemblies().ToArray();

            builder.RegisterAssemblyTypes(assemblies)
                .Where(type => type.IsConcretePublicClassImplementing<INuGetPackageRestoreFix>())
                .AsImplementedInterfaces();
        }
    }
}
