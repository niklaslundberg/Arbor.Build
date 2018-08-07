using System.Linq;
using System.Reflection;
using Arbor.X.Core.Assemblies;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Autofac;
using JetBrains.Annotations;
using Module = Autofac.Module;

namespace Arbor.X.Core.Configuration.AutofacModules
{
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

    public class BuildContext
    {
        public BuildConfiguration CurrentBuildConfiguration { get; set; }
    }

    public class BuildConfiguration
    {
        public string Configuration { get; }

        public BuildConfiguration(string configuration)
        {
            Configuration = configuration;
        }
    }

    [UsedImplicitly]
    public class BuildContextModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BuildContext>().AsSelf().SingleInstance();
        }
    }
}
