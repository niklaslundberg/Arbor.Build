using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Arbor.X.Core.Assemblies
{
    public static class AssemblyExtensions
    {
        public static bool IsDebugAssembly(this Assembly assembly)
        {
            var debuggableAttribute = assembly.GetCustomAttributes(typeof(DebuggableAttribute)).SingleOrDefault() as DebuggableAttribute;

            if (debuggableAttribute == null)
            {
                return false;
            }

            return debuggableAttribute.IsJITOptimizerDisabled;
        }
    }
}
