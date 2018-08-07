using Autofac;
using Serilog;
using Module = Autofac.Module;

namespace Arbor.X.Core
{
    public static partial class BuildBootstrapper
    {
        public class SerilogModule : Module
        {
            private readonly ILogger _logger;

            public SerilogModule(ILogger logger)
            {
                _logger = logger;
            }

            protected override void Load(ContainerBuilder builder)
            {
                builder.RegisterInstance(_logger).AsImplementedInterfaces().SingleInstance();
            }
        }
    }
}
