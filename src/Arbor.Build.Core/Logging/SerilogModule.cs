using System;
using Autofac;
using Serilog;

namespace Arbor.Build.Core.Logging;

public class SerilogModule(ILogger logger) : Module
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override void Load(ContainerBuilder builder) => builder.RegisterInstance(_logger)
        .AsImplementedInterfaces()
        .SingleInstance();
}