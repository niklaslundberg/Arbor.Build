using System.Collections.Generic;
using System.Reflection;
using Arbor.Defensive.Collections;
using Arbor.X.Core.GenericExtensions;

namespace Arbor.X.Core.Assemblies
{
    public static class AssemblyFetcher
    {
        public static IReadOnlyCollection<Assembly> GetAssemblies()
        {
            Assembly[] assemblies =
                {
                    Assembly.GetExecutingAssembly()
                };

            return assemblies.ToReadOnlyCollection();
        }
    }
}
