using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

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

            IEnumerable<Assembly> filtered = assemblies
                .Where(assembly =>
                    !assembly.IsDynamic && assembly.FullName.StartsWith("Arbor.X", StringComparison.Ordinal));

            return filtered.ToImmutableHashSet();
        }
    }
}
