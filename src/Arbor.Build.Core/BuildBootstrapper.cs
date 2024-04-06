using System;
using System.Linq;
using System.Threading.Tasks;
using Arbor.Build.Core.Assemblies;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.Logging;
using Autofac;
using Autofac.Core;
using Autofac.Util;
using Serilog;
using Zio;

namespace Arbor.Build.Core;

public static class BuildBootstrapper
{
    public static Task<IContainer> StartAsync(ILogger logger,
        IEnvironmentVariables environmentVariables,
        ISpecialFolders specialFolders,
        DirectoryEntry? sourceDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var builder = new ContainerBuilder();

        RegisterLogging(logger, builder);

        RegisterAssemblyModules(builder);

        RegisterSourceRootConditionally(sourceDirectory, builder);

        builder.RegisterInstance(environmentVariables).AsImplementedInterfaces();
        builder.RegisterInstance(specialFolders).AsImplementedInterfaces();

        IContainer container = builder.Build();

        return container.AsCompletedTask()!;
    }

    private static void RegisterSourceRootConditionally(DirectoryEntry? sourceDirectory, ContainerBuilder builder)
    {
        if (sourceDirectory is {})
        {
            builder.RegisterModule(new BuildVariableModule(sourceDirectory));
        }
    }

    private static void RegisterAssemblyModules(ContainerBuilder builder)
    {
        foreach (IModule module in GetModulesFromAssemblies())
        {
            builder.RegisterModule(module);
        }
    }

    private static void RegisterLogging(ILogger logger, ContainerBuilder builder) => builder.RegisterModule(new SerilogModule(logger));

    private static IModule[] GetModulesFromAssemblies()
    {
        var assemblies = AssemblyFetcher.GetFilteredAssemblies();

        return assemblies.SelectMany(assembly =>
                assembly.GetLoadableTypes()
                    .Where(type =>
                        type.IsConcretePublicClassImplementing<IModule>()
                        && type.HasSingleDefaultConstructor()))
            .Select(type => Activator.CreateInstance(type) as IModule)
            .NotNull()
            .ToArray();
    }
}