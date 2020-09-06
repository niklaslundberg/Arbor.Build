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
#pragma warning disable CA2000 // Dispose objects before losing scope
            IFileSystem fileSystem = new PhysicalJunctionFs(new WindowsFs(new PhysicalFileSystem()));
#pragma warning restore CA2000 // Dispose objects before losing scope
            builder.RegisterInstance(fileSystem);
        }
    }
}