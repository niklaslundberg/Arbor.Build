using System.Linq;
using Autofac;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NuGetPackageRestoreFixModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assemblies = AssemblyExtensions.GetAssemblies().ToArray();
            
            builder.RegisterAssemblyTypes(assemblies)
                .Where(type => type.IsConcretePublicClassImplementing<INuGetPackageRestoreFix>())
                .AsImplementedInterfaces();
        }
    }
}