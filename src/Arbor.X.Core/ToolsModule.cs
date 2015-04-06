using System.Linq;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Tools.Environments;
using Autofac;
using JetBrains.Annotations;

namespace Arbor.X.Core
{
    [UsedImplicitly]
    public class ToolsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assemblies = AssemblyExtensions.GetAssemblies().ToArray();

            builder.RegisterType<SourcePathVariableProvider>().AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(assemblies)
                .Where(type => type.IsConcretePublicClassImplementing<IVariableProvider>())
                .AsImplementedInterfaces();
        }
    }
}