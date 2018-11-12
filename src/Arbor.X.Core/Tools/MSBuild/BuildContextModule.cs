using Autofac;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
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
