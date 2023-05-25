using Autofac;
using Zio;

namespace Arbor.Build.Core.BuildVariables;

public class BuildVariableModule : Module
{
    private readonly DirectoryEntry _sourceDirectory;

    public BuildVariableModule(DirectoryEntry sourceDirectory) => _sourceDirectory = sourceDirectory;

    protected override void Load(ContainerBuilder builder) =>
        builder.RegisterInstance(new SourceRootValue(_sourceDirectory))
            .AsSelf()
            .SingleInstance();
}