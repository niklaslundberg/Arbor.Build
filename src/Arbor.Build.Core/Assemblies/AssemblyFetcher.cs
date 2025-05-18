using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Arbor.Build.Core.Assemblies;

public static class AssemblyFetcher
{
    public static readonly ImmutableHashSet<Assembly> FilteredAssemblies = GetAssemblies().ToImmutableHashSet();

    private static Assembly[] GetAssemblies()
    {
        var assemblies = new HashSet<Assembly>
        {
            Assembly.GetExecutingAssembly()
        };

        foreach (Assembly appDomainAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            assemblies.Add(appDomainAssembly);
        }

        IEnumerable<Assembly> filtered = assemblies
            .Where(assembly =>
                assembly is { IsDynamic: false, FullName: { } }
                && assembly.FullName.StartsWith(ArborConstants.ArborBuild, StringComparison.Ordinal));

        return filtered.ToArray();
    }
}