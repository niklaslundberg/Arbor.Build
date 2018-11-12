using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Arbor.Build.Core.BuildVariables;
using JetBrains.Annotations;
using Mono.Cecil;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Core.Assemblies
{
    public static class AssemblyExtensions
    {
        public static bool? IsDebugAssembly(
            [NotNull] this AssemblyDefinition assemblyDefinition,
            [NotNull] FileInfo fileInfo,
            ILogger logger = null)
        {
            if (assemblyDefinition == null)
            {
                throw new ArgumentNullException(nameof(assemblyDefinition));
            }

            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            string binDebugPath = $"bin{Path.DirectorySeparatorChar}debug";

            if (fileInfo.FullName.Contains(binDebugPath,
                StringComparison.OrdinalIgnoreCase))
            {
                logger?.Debug("Found assembly {Assembly} in {Path}, skipping assembly loading", fileInfo.FullName, binDebugPath);
                return true;
            }

            ILogger usedLogger = logger ?? Logger.None;

            Assembly loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => !assembly.IsDynamic
                                             && assembly.FullName.Equals(assemblyDefinition.FullName,
                                                 StringComparison.OrdinalIgnoreCase));

            if (loadedAssembly != null)
            {
                if (usedLogger.IsEnabled(LogEventLevel.Verbose))
                {
                    usedLogger.Debug("Assembly '{FullName}' is already loaded in the app domain",
                        assemblyDefinition.FullName);
                }

                return IsDebugAssembly(loadedAssembly, usedLogger);
            }

            Assembly loadedReflectionOnlyAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
                .SingleOrDefault(assembly => !assembly.IsDynamic
                                             && assembly.FullName.Equals(assemblyDefinition.FullName,
                                                 StringComparison.OrdinalIgnoreCase));

            if (loadedReflectionOnlyAssembly != null)
            {
                usedLogger.Debug("Assembly '{FullName}' is already loaded in the app domain with reflection only",
                    assemblyDefinition.FullName);

                return IsDebugAssembly(loadedReflectionOnlyAssembly, usedLogger);
            }

            bool verboseEnabled = usedLogger.IsEnabled(LogEventLevel.Verbose);
            if (!bool.TryParse(Environment.GetEnvironmentVariable(WellKnownVariables.AssemblyUseReflectionOnlyMode),
                    out bool enabled) || enabled)
            {
                try
                {
                    Assembly reflectedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fileInfo.FullName);;

                    if (reflectedAssembly != null)
                    {
                        bool? isDebugAssembly = IsDebugAssembly(reflectedAssembly, usedLogger);

                        if (verboseEnabled)
                        {
                            usedLogger.Verbose("Assembly is debug from reflected assembly: {IsDebug}",
                                isDebugAssembly?.ToString(CultureInfo.InvariantCulture) ?? "N/A");

                        }

                        return isDebugAssembly;
                    }

                    if (verboseEnabled)
                    {
                        usedLogger.Verbose("Reflected assembly from assembly definition {FullName} was null",
                            assemblyDefinition.FullName);
                    }
                }
                catch (Exception ex)
                {
                    if (verboseEnabled)
                    {
                        usedLogger.Verbose(ex,
                            "Error while getting reflected assembly definition from assembly definition {FullName}",
                            assemblyDefinition.FullName);
                    }

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
                if (verboseEnabled)
                {
                    usedLogger.Verbose(ex,
                        "Error while getting is debug from assembly definition {FullName}",
                        assemblyDefinition.FullName);
                }

                return null;
            }

            return false;
        }

        public static bool? IsDebugAssembly([NotNull] this Assembly assembly, [NotNull] ILogger usedLogger)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (usedLogger == null)
            {
                throw new ArgumentNullException(nameof(usedLogger));
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
                    bool isEnabled = usedLogger.IsEnabled(LogEventLevel.Verbose);
                    if (isEnabled)
                    {
                        usedLogger.Verbose(ex,
                            "Error while getting is debug from reflected assembly {FullName}",
                            assembly.FullName);
                    }

                    return null;
                }
            }

            bool? isDebugBuild = null;

            try
            {
                object[] attribs = assembly.GetCustomAttributes(debuggableAttributeType,
                    false);

                if (attribs.Length > 0)
                {
                    if (attribs[0] is DebuggableAttribute debuggableAttribute)
                    {
                        isDebugBuild = debuggableAttribute.IsJITOptimizerDisabled;
                    }
                }
                else
                {
                    isDebugBuild = false;
                }
            }
            catch (Exception ex)
            {
                if (usedLogger.IsEnabled(LogEventLevel.Verbose))
                {
                    usedLogger.Verbose(ex, "Error while is debug from assembly {FullName}", assembly.FullName);
                }
            }

            return isDebugBuild;
        }
    }
}
