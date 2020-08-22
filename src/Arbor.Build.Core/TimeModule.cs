using Arbor.FS;
using Autofac;
using JetBrains.Annotations;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Core
{
    [UsedImplicitly]
    public class TimeModule : Module
    {
        protected override void Load(ContainerBuilder builder) =>
            builder.RegisterType<TimeService>().AsImplementedInterfaces();
    }

    [UsedImplicitly]
    public class FileSystemModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
           IFileSystem fileSystem = new Arbor.FS.PhysicalJunctionFs(new WindowsFs(new PhysicalFileSystem()));
           builder.RegisterInstance(fileSystem);
        }
    }
}