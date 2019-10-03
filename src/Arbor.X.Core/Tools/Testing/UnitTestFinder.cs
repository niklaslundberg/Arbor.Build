using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Arbor.Build.Core.Assemblies;
using Mono.Cecil;
using Serilog;
using Serilog.Events;

namespace Arbor.Build.Core.Tools.Testing
{
    public sealed class UnitTestFinder
    {
        private const string DllSearchPattern = "*.dll";
        private const string GenericPartSeparator = "`";
        private static readonly string[] _IgnoredNames = { "ReSharper", "dotCover", "Microsoft" };

        private static readonly ImmutableHashSet<string> _Excluded = new[]
        {
            ".git",
            ".hg",
            ".svn",
            "obj",
            "build",
            "packages",
            "_ReSharper",
            "external",
            "artifacts",
            "temp",
            ".HistoryData",
            "LocalHistory",
            "_",
            ".",
            "NCrunch",
            ".vs",
            "publish"
        }.ToImmutableHashSet();

        private readonly ILogger _logger;
        private readonly IEnumerable<Type> _typesToFind;

        private readonly bool _debugLevelEnabled;
        private readonly bool _verboseLevelEnabled;

        public UnitTestFinder(IEnumerable<Type> typesToFind, bool debugLogEnabled = false, ILogger logger = null)
        {
            _logger = logger;
            _typesToFind = typesToFind;
            DebugLogEnabled = debugLogEnabled;

            _debugLevelEnabled = _logger?.IsEnabled(LogEventLevel.Debug) ?? false;
            _verboseLevelEnabled = _logger?.IsEnabled(LogEventLevel.Verbose) ?? false;
        }

        private bool DebugLogEnabled { get; }

        public HashSet<string> GetUnitTestFixtureDlls(
            DirectoryInfo currentDirectory,
            bool? releaseBuild = null,
            ImmutableArray<string> assemblyFilePrefix = default,
            string targetFrameworkPrefix = null,
            bool strictConfiguration = false)
        {
            if (currentDirectory == null)
            {
                throw new ArgumentNullException(nameof(currentDirectory));
            }

            string fullName = currentDirectory.FullName;

            if (!currentDirectory.Exists)
            {
                return new HashSet<string>();
            }

            bool isExcluded =
                _Excluded.Any(
                    excludedItem =>
                        currentDirectory.Name.StartsWith(excludedItem, StringComparison.OrdinalIgnoreCase));

            if (isExcluded)
            {
                if (_verboseLevelEnabled)
                {
                    _logger?.Verbose("Directory '{FullName}' is excluded", fullName);
                }

                return new HashSet<string>();
            }

            IEnumerable<FileInfo> filteredDllFiles = assemblyFilePrefix.IsDefaultOrEmpty
                ? currentDirectory.EnumerateFiles(DllSearchPattern)
                : currentDirectory.EnumerateFiles(DllSearchPattern)
                    .Where(file =>
                        assemblyFilePrefix.Any(prefix =>
                            file.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

            List<(AssemblyDefinition, FileInfo)> assemblies = filteredDllFiles
                .Where(file => !file.Name.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                .Where(file => !_IgnoredNames.Any(
                    name => file.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(dllFile => GetAssembly(dllFile, targetFrameworkPrefix))
                .Where(assembly => assembly.Item1 != null)
                .ToList();
            IReadOnlyCollection<string> testFixtureAssemblies;

            if (assemblies.Count > 0)
            {
                List<(AssemblyDefinition, FileInfo)> configurationFiltered;

                bool isReleaseBuild = releaseBuild.HasValue && releaseBuild.Value;
                bool isDebugBuild = releaseBuild.HasValue && !releaseBuild.Value;

                if (isReleaseBuild)
                {
                    var assembliesWithDebugFlag = assemblies
                        .Select(assembly => new
                        {
                            Assembly = assembly,
                            IsDebug = assembly.Item1.IsDebugAssembly(assembly.Item2, _logger)
                        }).ToArray();

                    configurationFiltered = assembliesWithDebugFlag
                        .Where(item => item.IsDebug == false)
                        .Select(item => item.Assembly)
                        .ToList();

                    string[] debugAssemblies = assembliesWithDebugFlag
                        .Where(item => item.IsDebug == true)
                        .Select(a => a.Assembly.Item1.FullName)
                        .ToArray();

                    var unknownAssemblies = assembliesWithDebugFlag
                        .Where(item => item.IsDebug is null)
                        .Select(a => a.Assembly)
                        .ToArray();

                    if (!strictConfiguration)
                    {
                        configurationFiltered.AddRange(unknownAssemblies);
                    }

                    if (debugAssemblies.Length > 0)
                    {
                        if (_debugLevelEnabled)
                        {
                            _logger?.Debug("Filtered out debug assemblies {DebugAssemblies}", debugAssemblies);
                        }
                    }
                }
                else if (isDebugBuild)
                {
                    var assembliesWithDebugFlag = assemblies
                        .Select(assembly => new
                        {
                            Assembly = assembly,
                            IsDebug = assembly.Item1.IsDebugAssembly(assembly.Item2, _logger)
                        }).ToArray();

                    configurationFiltered = assembliesWithDebugFlag
                        .Where(item => item.IsDebug == true)
                        .Select(item => item.Assembly)
                        .ToList();

                    List<string> nonDebugAssemblies = assembliesWithDebugFlag
                        .Where(item => item.IsDebug is null || item.IsDebug == false)
                        .Select(item => item.Assembly.Item1.FullName)
                        .ToList();

                    (AssemblyDefinition, FileInfo)[] unknownAssemblies = assembliesWithDebugFlag
                        .Where(item => item.IsDebug is null)
                        .Select(a => a.Assembly)
                        .ToArray();

                    if (!strictConfiguration)
                    {
                        configurationFiltered.AddRange(unknownAssemblies);
                    }

                    if (nonDebugAssemblies.Count > 0)
                    {
                        if (_debugLevelEnabled)
                        {
                            _logger?.Debug("Filtered out release assemblies {NonDebugAssemblies}", nonDebugAssemblies);
                        }
                    }
                }
                else
                {
                    configurationFiltered = assemblies;
                    if (_verboseLevelEnabled)
                    {
                        _logger?.Verbose("No debug/release filter is used");
                    }
                }

                testFixtureAssemblies = UnitTestFixtureAssemblies(configurationFiltered);
            }
            else
            {
                testFixtureAssemblies = Array.Empty<string>();
            }

            List<string> subDirAssemblies = currentDirectory
                .EnumerateDirectories()
                .SelectMany(dir => GetUnitTestFixtureDlls(dir, releaseBuild, assemblyFilePrefix, targetFrameworkPrefix))
                .ToList();

            HashSet<string> allUnitFixtureAssemblies = testFixtureAssemblies
                .Concat(subDirAssemblies)
                .Distinct()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return allUnitFixtureAssemblies;
        }

        public bool TryIsTypeTestFixture(TypeDefinition typeToInvestigate)
        {
            if (typeToInvestigate == null)
            {
                throw new ArgumentNullException(nameof(typeToInvestigate));
            }

            try
            {
                string toInvestigate = typeToInvestigate.FullName;
                bool any = IsTypeUnitTestFixture(typeToInvestigate);

                if (any)
                {
                    if (_debugLevelEnabled)
                    {
                        _logger?.Debug("Testing type '{ToInvestigate}': is unit test fixture", toInvestigate);
                    }
                }

                return any;
            }
            catch (Exception ex)
            {
                if (_debugLevelEnabled)
                {
                    _logger?.Debug("Failed to determine if type {FullName} is {V} {Message}",
                        typeToInvestigate.Module.Assembly.FullName,
                        string.Join(" | ", _typesToFind.Select(type => type.FullName)),
                        ex.Message);
                }

                return false;
            }
        }

// ReSharper disable ReturnTypeCanBeEnumerable.Local
        private IReadOnlyCollection<string> UnitTestFixtureAssemblies(
                IEnumerable<(AssemblyDefinition, FileInfo)> assemblies)

            // ReSharper restore ReturnTypeCanBeEnumerable.Local
        {
            List<string> unitTestFixtureAssemblies =
                assemblies.Where(TryFindAssembly)
                    .Select(a => a.Item2.FullName)
                    .Distinct()
                    .ToList();

            return unitTestFixtureAssemblies;
        }

        private bool TryFindAssembly((AssemblyDefinition, FileInfo) assembly)
        {
            bool result;
            try
            {
                if (DebugLogEnabled)
                {
                    _logger?.Debug("Testing assembly '{Assembly}'", assembly);
                }

                TypeDefinition[] types = assembly.Item1.MainModule.Types.ToArray();
                bool anyType = types.Any(TryIsTypeTestFixture);

                result = anyType;
            }
            catch (Exception)
            {
                if (DebugLogEnabled)
                {
                    _logger?.Debug("Could not get types from assembly '{FullName}'", assembly.Item1.FullName);
                }

                result = false;
            }

            if (DebugLogEnabled || result)
            {
                if (_debugLevelEnabled)
                {
                    _logger?.Debug("Assembly {FullName}, found any class with {V}: {Result}",
                        assembly.Item1.FullName,
                        string.Join(" | ", _typesToFind.Select(type => type.FullName)),
                        result);
                }
            }

            return result;
        }

        private bool IsTypeUnitTestFixture(TypeDefinition typeToInvestigate)
        {
            IEnumerable<CustomAttribute> customAttributeDatas = typeToInvestigate.CustomAttributes;

            bool isTypeUnitTestFixture = IsCustomAttributeOfExpectedType(customAttributeDatas);

            bool isTestType = isTypeUnitTestFixture || TypeHasTestMethods(typeToInvestigate);

            return isTestType;
        }

        private bool IsCustomAttributeOfExpectedType(IEnumerable<CustomAttribute> customAttributes)
        {
            bool isTypeUnitTestFixture = customAttributes.Any(
                attributeData =>
                {
                    if (attributeData.AttributeType.FullName.StartsWith(nameof(System), StringComparison.Ordinal) ||
                        attributeData.AttributeType.FullName.StartsWith("_", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    return IsCustomAttributeTypeToFind(attributeData);
                });
            return isTypeUnitTestFixture;
        }

        private bool TypeHasTestMethods(TypeDefinition typeToInvestigate)
        {
            IEnumerable<MethodDefinition> publicInstanceMethods =
                typeToInvestigate.Methods.Where(method => method.IsPublic && !method.IsStatic);

            bool hasPublicInstanceTestMethod =
                publicInstanceMethods.Any(method => IsCustomAttributeOfExpectedType(method.CustomAttributes));

            FieldDefinition[] declaredFields = typeToInvestigate.Fields.ToArray();

            bool hasPrivateFieldMethod = declaredFields
                .Where(field => field.IsPrivate)
                .Any(
                    field =>
                    {
                        string fullName = field.FieldType.FullName;

                        bool any = _typesToFind.Any(
                            type =>
                                !string.IsNullOrWhiteSpace(fullName) && type.FullName == fullName);

                        if (field.FieldType.IsGenericInstance && !string.IsNullOrWhiteSpace(fullName))
                        {
                            int fieldIndex = fullName.IndexOf(
                                GenericPartSeparator,
                                StringComparison.OrdinalIgnoreCase);

                            string fieldName = fullName.Substring(0, fieldIndex);

                            return _typesToFind.Any(
                                type =>
                                {
                                    int typePosition = type.FullName?.IndexOf(
                                                           GenericPartSeparator,
                                                           StringComparison.OrdinalIgnoreCase) ?? -1;

                                    if (typePosition < 0)
                                    {
                                        return false;
                                    }

                                    string typeName = type.FullName?.Substring(0, typePosition) ?? "";

                                    return typeName.Equals(fieldName, StringComparison.Ordinal);
                                });
                        }

                        return any;
                    });

            bool hasTestMethod = hasPublicInstanceTestMethod || hasPrivateFieldMethod;

            return hasTestMethod;
        }

        private bool IsCustomAttributeTypeToFind(CustomAttribute attr) => _typesToFind.Any(
            typeToFind =>
                attr.AttributeType.FullName.Equals(
                    typeToFind.FullName,
                    StringComparison.OrdinalIgnoreCase));

        private (AssemblyDefinition, FileInfo) GetAssembly(FileInfo dllFile, string targetFrameworkPrefix)
        {
            try
            {
                AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(dllFile.FullName);

                if (!string.IsNullOrWhiteSpace(targetFrameworkPrefix))
                {
                    TargetFrameworkAttribute targetFrameworkAttribute = assemblyDefinition.CustomAttributes
                        .OfType<TargetFrameworkAttribute>().FirstOrDefault();

                    if (targetFrameworkAttribute != null)
                    {
                        if (!targetFrameworkAttribute.FrameworkName.StartsWith(targetFrameworkPrefix,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            if (_debugLevelEnabled)
                            {
                                _logger?.Debug(
                                    "The current assembly '{FullName}' target framework attribute with value '{FrameworkName}' does not match the specified target framework '{TargetFrameworkPrefix}'",
                                    dllFile.FullName,
                                    targetFrameworkAttribute.FrameworkName,
                                    targetFrameworkPrefix);
                            }

                            return (null, null);
                        }
                    }
                }

                TypeDefinition[] types = assemblyDefinition.Modules.SelectMany(m => m.GetTypes()).ToArray();

                int count = types.Length;

                if (DebugLogEnabled)
                {
                    if (_verboseLevelEnabled)
                    {
                        _logger?.Verbose("Found {Count} types in assembly '{FullName}'", count, dllFile.FullName);
                    }
                }

                return (assemblyDefinition, dllFile);
            }
            catch (ReflectionTypeLoadException ex)
            {
                string message = $"Could not load assembly '{dllFile.FullName}', type load exception. Ignoring.";

                if (_debugLevelEnabled)
                {
                    _logger?.Debug("{Message}", message);
                }
#if DEBUG
                Debug.WriteLine("{0}, {1}", message, ex);
#endif
                return (null, null);
            }
            catch (BadImageFormatException ex)
            {
                string message = $"Could not load assembly '{dllFile.FullName}', bad image format exception. Ignoring.";

                if (_debugLevelEnabled)
                {
                    _logger?.Debug("{Message}", message);
                }
#if DEBUG
                Debug.WriteLine("{0}, {1}", message, ex);
#endif
                return (null, null);
            }
        }
    }
}
