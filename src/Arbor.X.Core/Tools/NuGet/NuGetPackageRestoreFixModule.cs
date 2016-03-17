﻿using System.Linq;

using Arbor.X.Core.Assemblies;
using Arbor.X.Core.GenericExtensions;

using Autofac;

using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.NuGet
{
    [UsedImplicitly]
    public class NuGetPackageRestoreFixModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assemblies = AssemblyFetcher.GetAssemblies().ToArray();

            builder.RegisterAssemblyTypes(assemblies)
                .Where(type => type.IsConcretePublicClassImplementing<INuGetPackageRestoreFix>())
                .AsImplementedInterfaces();
        }
    }
}