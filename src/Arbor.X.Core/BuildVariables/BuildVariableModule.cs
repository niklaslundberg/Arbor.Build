using System;
using Autofac;
using JetBrains.Annotations;

namespace Arbor.Build.Core.BuildVariables
{
    public class BuildVariableModule : Module
    {
        private readonly string _sourceDirectory;

        public BuildVariableModule([NotNull] string sourceDirectory)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sourceDirectory));
            }

            _sourceDirectory = sourceDirectory;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new SourceRootValue(_sourceDirectory))
                .AsSelf()
                .SingleInstance();
        }
    }
}
