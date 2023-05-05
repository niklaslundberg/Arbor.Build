using System;
using Autofac;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Logging;

public class SerilogModule : Module
{
    private readonly ILogger _logger;

    public SerilogModule([NotNull] ILogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override void Load(ContainerBuilder builder) => builder.RegisterInstance(_logger)
        .AsImplementedInterfaces()
        .SingleInstance();
}