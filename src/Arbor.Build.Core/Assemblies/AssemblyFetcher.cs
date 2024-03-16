using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Arbor.Build.Core.GenericExtensions;

namespace Arbor.Build.Core.Assemblies;

public static class AssemblyFetcher
{
    public static ImmutableHashSet<Assembly> GetFilteredAssemblies()
    {
        var assemblies = new HashSet<Assembly>
        {
            Assembly.GetExecutingAssembly()
        };

        foreach (Assembly appDomainAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            assemblies.Add(appDomainAssembly);
        }

        string[] allowed = [ArborConstants.ArborX, ArborConstants.ArborBuild];

        IEnumerable<Assembly> filtered = assemblies
            .Where(assembly =>
                assembly is { IsDynamic: false, FullName: { } }
                && assembly.FullName.StartsWithAny(allowed, StringComparison.Ordinal));

        return filtered.ToImmutableHashSet();
    }
}