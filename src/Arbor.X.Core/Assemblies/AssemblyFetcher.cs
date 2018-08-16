using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Arbor.X.Core.GenericExtensions;

namespace Arbor.X.Core.Assemblies
{
    public static class AssemblyFetcher
    {
        public static ImmutableHashSet<Assembly> GetFilteredAssemblies()
        {
            var assemblies = new HashSet<Assembly>
            {
                Assembly.GetExecutingAssembly()
            };

            Assembly[] appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly appDomainAssembly in appDomainAssemblies)
            {
                assemblies.Add(appDomainAssembly);
            }

            string[] allowed = { ArborConstants.ArborX, ArborConstants.ArborBuild };

            IEnumerable<Assembly> filtered = assemblies
                .Where(assembly =>
                    !assembly.IsDynamic && assembly.FullName.StartsWithAny(allowed, StringComparison.Ordinal));

            return filtered.ToImmutableHashSet();
        }
    }
}
