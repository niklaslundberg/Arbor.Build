using Autofac;
using JetBrains.Annotations;

namespace Arbor.Build.Core
{
    [UsedImplicitly]
    public class TimeModule : Module
    {
        protected override void Load(ContainerBuilder builder) =>
            builder.RegisterType<TimeService>().AsImplementedInterfaces();
    }
}