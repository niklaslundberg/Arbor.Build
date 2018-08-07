using Autofac;
using JetBrains.Annotations;

namespace Arbor.X.Core.Configuration.AutofacModules
{
    [UsedImplicitly]
    public class BuildContextModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<BuildContext>().AsSelf().SingleInstance();
        }
    }
}