using System.Collections.Generic;
using System.Reflection;

namespace Arbor.X.Core
{
    public static class AssemblyExtensions
    {
        public static IReadOnlyCollection<Assembly> GetAssemblies()
        {

            Assembly[] assemblies = { Assembly.GetExecutingAssembly() };

            return assemblies.ToReadOnlyCollection();
        }
    }
}