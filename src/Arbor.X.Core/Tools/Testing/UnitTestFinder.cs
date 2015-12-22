﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Alphaleonis.Win32.Filesystem;

using Arbor.X.Core.Assemblies;
using Arbor.X.Core.Logging;
using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.Testing
{
    public class UnitTestFinder
    {
        readonly IEnumerable<Type> _typesToFind;
        readonly ILogger _logger;

        public UnitTestFinder(IEnumerable<Type> typesesToFind, bool debugLogEnabled = false, ILogger logger = null)
        {
            _logger = logger ?? new NullLogger();
            _typesToFind = typesesToFind;
            DebugLogEnabled = debugLogEnabled;
        }

        bool DebugLogEnabled { get; }

        public IReadOnlyCollection<string> GetUnitTestFixtureDlls(DirectoryInfo currentDirectory, bool? releaseBuild = null)
        {
            if (currentDirectory == null)
            {
                throw new ArgumentNullException(nameof(currentDirectory));
            }

            string fullName = currentDirectory.FullName;

            if (!currentDirectory.Exists)
            {
                return new ReadOnlyCollection<string>(new List<string>());
            }

            var blacklisted = new List<string> {".git", ".hg", ".svn", "obj", "build", "packages", "_ReSharper", "external", "artifacts", "temp", ".HistoryData", "LocalHistory", "_", ".", "NCrunch", ".vs"};

            bool isBlacklisted =
                blacklisted.Any(
                    blackListedItem =>
                        currentDirectory.Name.StartsWith(blackListedItem, StringComparison.InvariantCultureIgnoreCase));

            if (isBlacklisted)
            {
                _logger.WriteDebug($"Directory '{fullName}' is blacklisted");
                return new ReadOnlyCollection<string>(new List<string>());
            }

            var dllFiles = currentDirectory.EnumerateFiles("*.dll");

            var ignoredNames = new List<string> {"ReSharper", "dotCover", "Microsoft"};

            var assemblies = dllFiles
                .Where(file => !file.Name.StartsWith("System", StringComparison.InvariantCultureIgnoreCase))
                .Where(file => !ignoredNames.Any(name => file.Name.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) >= 0))
                .Select(GetAssembly)
                .Where(assembly => assembly != null)
                .Distinct()
                .ToList();

            List<Assembly> configurationFiltered;

            if (releaseBuild.HasValue && releaseBuild.Value)
            {
                configurationFiltered = assemblies.Where(assembly => !assembly.IsDebugAssembly()).ToList();
            }
            else if (releaseBuild.HasValue)
            {
                configurationFiltered = assemblies.Where(assembly => assembly.IsDebugAssembly()).ToList();
            }
            else
            {
                configurationFiltered = assemblies;
            }

            var testFixtureAssemblies = UnitTestFixtureAssemblies(configurationFiltered);

            var subDirAssemblies = currentDirectory
                .EnumerateDirectories()
                .SelectMany(dir =>GetUnitTestFixtureDlls(dir, releaseBuild));

            var allUnitFixtureAssemblies = testFixtureAssemblies
                .Concat(subDirAssemblies)
                .Distinct()
                .ToList();

            return allUnitFixtureAssemblies;
        }

// ReSharper disable ReturnTypeCanBeEnumerable.Local
        IReadOnlyCollection<string> UnitTestFixtureAssemblies(IEnumerable<Assembly> assemblies)
// ReSharper restore ReturnTypeCanBeEnumerable.Local
        {
            var unitTestFixtureAssemblies =
                assemblies.Where(TryFindAssembly)
                    .Select(a => a.Location)
                    .Distinct()
                    .ToList();
            return unitTestFixtureAssemblies;
        }

        bool TryFindAssembly(Assembly assembly)
        {
            bool result;
            try
            {
                _logger.WriteDebug($"Testing assembly '{assembly}'");
                Type[] types = assembly.GetExportedTypes();
                var anyType = types.Any(TryIsTypeTestFixture);

                result = anyType;
            }
            catch (Exception)
            {
                _logger.WriteDebug($"Could not get types from assembly '{assembly.FullName}'");
                result = false;
            }

            if (DebugLogEnabled || result)
            {
                _logger.WriteDebug(
                    $"Assembly {assembly.FullName}, found any class with {string.Join(" | ", _typesToFind.Select(type => type.FullName))}: {result}");
            }

            return result;
        }

        public bool TryIsTypeTestFixture(Type typeToInvestigate)
        {
            if (typeToInvestigate == null)
            {
                throw new ArgumentNullException(nameof(typeToInvestigate));
            }

            try
            {
                var toInvestigate = typeToInvestigate.FullName;
                var any = IsTypeUnitTestFixture(typeToInvestigate);

                if (any)
                {
                    _logger.WriteDebug($"Testing type '{toInvestigate}': is unit test fixture");
                }

                return any;
            }
            catch (Exception ex)
            {
                _logger.WriteDebug(
                    $"Failed to determine if type {typeToInvestigate.AssemblyQualifiedName} is {string.Join(" | ", _typesToFind.Select(type => type.FullName))} {ex.Message}");
                return false;
            }
        }

        bool IsTypeUnitTestFixture(Type typeToInvestigate)
        {
            var customAttributeDatas = typeToInvestigate.CustomAttributes;

            var isTypeUnitTestFixture = IsCustomAttributeOfExpectedType(customAttributeDatas);

            bool isTestType = isTypeUnitTestFixture || TypeHasTestMethods(typeToInvestigate);

            return isTestType;
        }

        bool IsCustomAttributeOfExpectedType(IEnumerable<CustomAttributeData> customAttributeDatas)
        {
            var isTypeUnitTestFixture = customAttributeDatas.Any(
                attr =>
                {
                    if (attr.AttributeType.FullName.StartsWith("System") || attr.AttributeType.FullName.StartsWith("_"))
                    {
                        return false;
                    }

                    return IsCustomAttributeTypeToFind(attr);
                });
            return isTypeUnitTestFixture;
        }

        bool TypeHasTestMethods(Type typeToInvestigate)
        {
            var publicInstanceMethods =
                typeToInvestigate.GetTypeInfo().GetMethods().Where(method => method.IsPublic && !method.IsStatic);

            var hasPublicInstanceTestMethod =
                publicInstanceMethods.Any(method => IsCustomAttributeOfExpectedType(method.CustomAttributes));

            var declaredFields = typeToInvestigate.GetTypeInfo().DeclaredFields.ToArray();

            bool hasPrivateFieldMethod = declaredFields
                .Where(field => field.IsPrivate)
                .Any(
                    field =>
                    {
                        string fullName = field.FieldType.FullName;

                        var any = _typesToFind.Any(
                            type =>
                                !string.IsNullOrWhiteSpace(fullName) && type.FullName == fullName);

                        if (field.FieldType.IsGenericType && !string.IsNullOrWhiteSpace(fullName))
                        {
                            const string GenericPartSeparator = "`";
                            var fieldIndex = fullName.IndexOf(GenericPartSeparator,
                                StringComparison.InvariantCultureIgnoreCase);

                            var fieldName = fullName.Substring(0, fieldIndex);

                            return _typesToFind.Any(
                                type =>
                                {
                                    var typePosition = type.FullName.IndexOf(GenericPartSeparator,
                                        StringComparison.InvariantCultureIgnoreCase);

                                    if (typePosition < 0)
                                    {
                                        return false;
                                    }

                                    var typeName = type.FullName.Substring(0, typePosition);

                                    return typeName.Equals(fieldName);
                                });
                        }

                        return any;
                    });

            bool hasTestMethod = hasPublicInstanceTestMethod || hasPrivateFieldMethod;

            return hasTestMethod;
        }

        bool IsCustomAttributeTypeToFind(CustomAttributeData attr)
        {
            return
                _typesToFind.Any(
                    typeToFind =>
                        attr.AttributeType.FullName.Equals(typeToFind.FullName,
                            StringComparison.InvariantCultureIgnoreCase));
        }

        Assembly GetAssembly(FileInfo dllFile)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile.FullName);

                var types = assembly.GetTypes();

                int count = types.Count();

                if (DebugLogEnabled)
                {
                    _logger.WriteVerbose($"Found {count} types in assembly '{assembly.Location}'");
                }

                return assembly;
            }
            catch (ReflectionTypeLoadException ex)
            {
                string message = $"Could not load assembly '{dllFile.FullName}', type load exception. Ignoring.";

                _logger.WriteDebug(message);
#if DEBUG
                Debug.WriteLine( "{0}, {1}", message, ex);
#endif
                return null;
            }
            catch (BadImageFormatException ex)
            {
                string message = $"Could not load assembly '{dllFile.FullName}', bad image format exception. Ignoring.";

                _logger.WriteDebug(message);
#if DEBUG
                Debug.WriteLine("{0}, {1}", message, ex);
#endif
                return null;
            }
        }
    }
}
