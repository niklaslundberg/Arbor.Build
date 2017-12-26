using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Arbor.X.Core.Assemblies
{
    public static class AssemblyExtensions
    {
        public static bool IsDebugAssembly(this AssemblyDefinition assemblyDefinition)
        {
            Type type = typeof(DebuggableAttribute);

            CustomAttribute customAttribute = assemblyDefinition.CustomAttributes.SingleOrDefault(s => s.AttributeType.FullName == type.FullName);

            if (customAttribute is null)
            {
                return false;
            }

            return true;

        }

        public static bool IsDebugAssembly(this Assembly assembly)
        {
            if (!(assembly.GetCustomAttributes(typeof(DebuggableAttribute)).SingleOrDefault() is DebuggableAttribute debuggableAttribute))
            {
                return false;
            }

            return debuggableAttribute.IsJITOptimizerDisabled;
        }
    }
}
