using Autofac;

namespace Arbor.X.Core
{
    public class BuildVariableModule : Module
    {
        private readonly string _sourceDirectory;

        public BuildVariableModule(string sourceDirectory)
        {
            _sourceDirectory = sourceDirectory;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new SourceRootValue(_sourceDirectory))
                .AsSelf()
                .SingleInstance();
        }
    }

    public class SourceRootValue
    {
        public string SourceRoot { get; }

        public SourceRootValue(string sourceRoot)
        {
            SourceRoot = sourceRoot;
        }
    }
}
