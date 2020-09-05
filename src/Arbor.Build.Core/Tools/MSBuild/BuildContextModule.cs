using Arbor.Build.Core.Tools.NuGet;
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
            builder.RegisterType<NuGetPackager>().AsSelf().SingleInstance();
            builder.RegisterType<ManitestReWriter>().AsSelf().SingleInstance();
        }
    }
}
