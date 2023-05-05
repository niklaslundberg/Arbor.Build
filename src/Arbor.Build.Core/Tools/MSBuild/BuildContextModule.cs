using Arbor.Build.Core.Tools.NuGet;
using Autofac;
using JetBrains.Annotations;
using Zio;

namespace Arbor.Build.Core.Tools.MSBuild;

[UsedImplicitly]
public class BuildContextModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(context => new BuildContext(context.Resolve<IFileSystem>())).AsSelf().SingleInstance();
        builder.RegisterType<NuGetPackager>().AsSelf().SingleInstance();
        builder.RegisterType<ManifestReWriter>().AsSelf().SingleInstance();
    }
}