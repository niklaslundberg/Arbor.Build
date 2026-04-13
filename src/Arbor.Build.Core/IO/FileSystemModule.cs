using Autofac;
using JetBrains.Annotations;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Core.IO;

[UsedImplicitly]
public class FileSystemModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        IFileSystem fileSystem = new PhysicalFileSystem();
        builder.RegisterInstance(fileSystem);
    }
}