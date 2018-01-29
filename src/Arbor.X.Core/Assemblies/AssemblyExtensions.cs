using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using FubuCore;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Arbor.X.Core.Assemblies
{
    public static class AssemblyExtensions
    {
        public static bool IsDebugAssembly(
            [NotNull] this AssemblyDefinition assemblyDefinition,
            [NotNull] FileInfo fileInfo,
            [NotNull] ILogger logger)
        {
            if (assemblyDefinition == null)
            {
                throw new ArgumentNullException(nameof(assemblyDefinition));
            }

            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Assembly loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => !assembly.IsDynamic
                                             && assembly.FullName.Equals(assemblyDefinition.FullName));

            if (loadedAssembly != null)
            {
                logger.WriteDebug($"Assembly '{assemblyDefinition.FullName}' is already loaded in the app domain");

                return IsDebugAssembly(loadedAssembly);
            }

            Assembly loadedReflectionOnlyAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
                .SingleOrDefault(assembly => !assembly.IsDynamic
                                             && assembly.FullName.Equals(assemblyDefinition.FullName));

            if (loadedReflectionOnlyAssembly != null)
            {
                logger.WriteDebug($"Assembly '{assemblyDefinition.FullName}' is already loaded in the app domain with reflection only");

                return IsDebugAssembly(loadedReflectionOnlyAssembly);
            }

            if (!bool.TryParse(Environment.GetEnvironmentVariable(WellKnownVariables.AssemblyUseReflectionOnlyMode),
                    out bool enabled) || enabled)
            {
                try
                {
                    byte[] assemblyBytes;

                    using (var fs = new FileStream(fileInfo.FullName, FileMode.Open))
                    {
                        assemblyBytes = fs.ReadAllBytes();
                    }

                    Assembly reflectedAssembly = Assembly.ReflectionOnlyLoad(assemblyBytes);

                    if (reflectedAssembly != null)
                    {
                        bool isDebugAssembly = IsDebugAssembly(reflectedAssembly);

                        logger?.WriteVerbose($"Assembly is debug from reflected assembly: {isDebugAssembly}");

                        return isDebugAssembly;
                    }

                    logger?.WriteVerbose($"Reflected assembly from assembly definition {assemblyDefinition.FullName} was null");
                }
                catch (Exception ex)
                {
                    logger?.WriteError($"Error while getting reflected assembly definition from assembly definition {assemblyDefinition.FullName} {ex}");
                }
            }

            Type type = typeof(DebuggableAttribute);

            CustomAttribute customAttribute = assemblyDefinition.CustomAttributes.SingleOrDefault(s => s.AttributeType.FullName == type.FullName);

            if (customAttribute is null)
            {
                return false;
            }

            return true;
        }

        public static bool IsDebugAssembly([NotNull] this Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            Type debuggableAttributeType = typeof(DebuggableAttribute);

            if (assembly.ReflectionOnly)
            {
                IList<CustomAttributeData> customAttributeDatas = CustomAttributeData.GetCustomAttributes(assembly);

                CustomAttributeData customAttributeData = customAttributeDatas.SingleOrDefault(cat => cat.AttributeType == debuggableAttributeType);

                if (customAttributeData != null)
                {
                    foreach (CustomAttributeTypedArgument cata in customAttributeData.ConstructorArguments)
                    {
                        if (cata.Value.GetType() != typeof(ReadOnlyCollection<CustomAttributeTypedArgument>))
                        {
                            bool isDebugAssembly = (uint)(((DebuggableAttribute.DebuggingModes)cata.Value) & DebuggableAttribute.DebuggingModes.Default) > 0U;

                            return isDebugAssembly;
                        }
                    }
                }
            }

            object[] attribs = assembly.GetCustomAttributes(debuggableAttributeType,
                                                        false);

            bool HasDebuggableAttribute;
            bool IsJITOptimized;
            string DebugOutput;
            bool isDebugBuild = false;

            if (attribs.Length > 0)
            {
                if (attribs[0] is DebuggableAttribute debuggableAttribute)
                {
                    HasDebuggableAttribute = true;
                    IsJITOptimized = !debuggableAttribute.IsJITOptimizerDisabled;
                    isDebugBuild = debuggableAttribute.IsJITOptimizerDisabled;

                    DebugOutput = (debuggableAttribute.DebuggingFlags &
                                    DebuggableAttribute.DebuggingModes.Default) !=
                                    DebuggableAttribute.DebuggingModes.None
                                    ? "Full" : "pdb-only";
                }
            }
            else
            {
                IsJITOptimized = true;
                isDebugBuild = false;
            }

            return isDebugBuild;
        }
    }
}
