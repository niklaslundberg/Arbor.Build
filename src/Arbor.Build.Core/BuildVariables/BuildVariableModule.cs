using System;
using Autofac;
using JetBrains.Annotations;
using Zio;

namespace Arbor.Build.Core.BuildVariables
{
    public class BuildVariableModule : Module
    {
        private readonly DirectoryEntry _sourceDirectory;

        public BuildVariableModule([NotNull] DirectoryEntry sourceDirectory)
        {
            if (sourceDirectory == null)
            {
                throw new ArgumentNullException(nameof(sourceDirectory));
            }

            _sourceDirectory = sourceDirectory;
        }

        protected override void Load(ContainerBuilder builder) =>
            builder.RegisterInstance(new SourceRootValue(_sourceDirectory))
                .AsSelf()
                .SingleInstance();
    }
}
