using Autofac;
using Zio;

namespace Arbor.Build.Core.BuildVariables;

public class BuildVariableModule(DirectoryEntry sourceDirectory) : Module
{
    protected override void Load(ContainerBuilder builder) =>
        builder.RegisterInstance(new SourceRootValue(sourceDirectory))
            .AsSelf()
            .SingleInstance();
}