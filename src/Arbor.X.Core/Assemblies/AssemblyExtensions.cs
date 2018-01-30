using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using FubuCore;
using JetBrains.Annotations;
using Mono.Cecil;

namespace Arbor.X.Core.Assemblies
{
    public static class AssemblyExtensions
    {
        public static bool? IsDebugAssembly(
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
                                             && assembly.FullName.Equals(assemblyDefinition.FullName,
                                                 StringComparison.OrdinalIgnoreCase));

            if (loadedAssembly != null)
            {
                logger.WriteDebug($"Assembly '{assemblyDefinition.FullName}' is already loaded in the app domain");

                return IsDebugAssembly(loadedAssembly, logger);
            }

            Assembly loadedReflectionOnlyAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
                .SingleOrDefault(assembly => !assembly.IsDynamic
                                             && assembly.FullName.Equals(assemblyDefinition.FullName,
                                                 StringComparison.OrdinalIgnoreCase));

            if (loadedReflectionOnlyAssembly != null)
            {
                logger.WriteDebug(
                    $"Assembly '{assemblyDefinition.FullName}' is already loaded in the app domain with reflection only");

                return IsDebugAssembly(loadedReflectionOnlyAssembly, logger);
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
                        bool? isDebugAssembly = IsDebugAssembly(reflectedAssembly, logger);

                        logger.WriteVerbose(
                            $"Assembly is debug from reflected assembly: {isDebugAssembly?.ToString(CultureInfo.InvariantCulture) ?? "N/A"}");

                        return isDebugAssembly;
                    }

                    logger.WriteVerbose(
                        $"Reflected assembly from assembly definition {assemblyDefinition.FullName} was null");
                }
                catch (Exception ex)
                {
                    logger.WriteError(
                        $"Error while getting reflected assembly definition from assembly definition {assemblyDefinition.FullName} {ex}");
                    return null;
                }
            }

            try
            {
                Type type = typeof(DebuggableAttribute);

                CustomAttribute customAttribute =
                    assemblyDefinition.CustomAttributes.SingleOrDefault(s => s.AttributeType.FullName == type.FullName);

                if (customAttribute != null)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.WriteError(
                    $"Error while getting is debug from assembly definition {assemblyDefinition.FullName} {ex}");
                return null;
            }

            return false;
        }

        public static bool? IsDebugAssembly([NotNull] this Assembly assembly, [NotNull] ILogger logger)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Type debuggableAttributeType = typeof(DebuggableAttribute);

            if (assembly.ReflectionOnly)
            {
                try
                {
                    IList<CustomAttributeData> customAttributeDatas = CustomAttributeData.GetCustomAttributes(assembly);

                    CustomAttributeData customAttributeData =
                        customAttributeDatas.SingleOrDefault(cat => cat.AttributeType == debuggableAttributeType);

                    if (customAttributeData != null)
                    {
                        foreach (CustomAttributeTypedArgument cata in customAttributeData.ConstructorArguments)
                        {
                            if (cata.Value.GetType() != typeof(ReadOnlyCollection<CustomAttributeTypedArgument>))
                            {
                                bool isDebugAssembly =
                                    (uint)(((DebuggableAttribute.DebuggingModes)cata.Value) &
                                           DebuggableAttribute.DebuggingModes.Default) > 0U;

                                return isDebugAssembly;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteError($"Error while getting is debug from reflected assembly {assembly.FullName} {ex}");

                    return null;
                }
            }

            bool? isDebugBuild = null;

            try
            {
                object[] attribs = assembly.GetCustomAttributes(debuggableAttributeType,
                    false);

                bool HasDebuggableAttribute;
                bool IsJITOptimized;
                string DebugOutput;

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
                            ? "Full"
                            : "pdb-only";
                    }
                }
                else
                {
                    IsJITOptimized = true;
                    isDebugBuild = false;
                }
            }
            catch (Exception ex)
            {
                logger.WriteError($"Error while is debug from assembly {assembly.FullName} {ex}");
            }

            return isDebugBuild;
        }
    }
}
