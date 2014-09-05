using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Arbor.Aesculus.Core;
using Arbor.X.Core.Logging;
using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.Testing
{
    public class UnitTestFinder
    {
        readonly IEnumerable<Type> _typesToFind;
        readonly ILogger _logger;

        public UnitTestFinder(IEnumerable<Type> typesesToFind, bool debugEnabled = false, ILogger logger = null)
        {
            _logger = logger ?? new NullLogger();
            _typesToFind = typesesToFind;
            DebugEnabled = debugEnabled;
        }

        bool DebugEnabled { get; set; }

        public IReadOnlyCollection<string> GetUnitTestFixtureDlls(DirectoryInfo currentDirectory)
        {
            if (currentDirectory == null)
            {
                throw new ArgumentNullException("currentDirectory");
            }

            string fullName = currentDirectory.FullName;

            if (!currentDirectory.Exists)
            {
                return new ReadOnlyCollection<string>(new List<string>());
            }

            var blacklisted = new List<string> {".git", ".hg", "obj", "packages"};

            bool isBlacklisted =
                blacklisted.Any(
                    blackListedItem =>
                        currentDirectory.Name.Equals(blackListedItem, StringComparison.InvariantCultureIgnoreCase));
            
            if (isBlacklisted)
            {
                _logger.WriteDebug(string.Format("Directory '{0}' is blacklisted", fullName));
                return new ReadOnlyCollection<string>(new List<string>());
            }

            var dllFiles = currentDirectory.EnumerateFiles("*.dll");

            var assemblies = dllFiles
                .Select(GetAssembly)
                .Where(assembly => assembly != null)
                .Distinct()
                .ToList();

            var testFixtureAssemblies = UnitTestFixtureAssemblies(assemblies);

            var subDirAssemblies = currentDirectory
                .EnumerateDirectories()
                .SelectMany(GetUnitTestFixtureDlls);

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
            var nunitFixtureAssemblies =
                assemblies.Where(TryFindAssembly)
                    .Select(a => a.Location)
                    .Distinct()
                    .ToList();
            return nunitFixtureAssemblies;
        }

        bool TryFindAssembly(Assembly assembly)
        {
            bool result;
            try
            {
                _logger.WriteVerbose(string.Format("Testing assembly '{0}'", assembly));
                Type[] types = assembly.GetExportedTypes();
                var anyType = types.Any(TryIsTypeTestFixture);

                result = anyType;
            }
            catch (Exception)
            {
                _logger.WriteVerbose(string.Format("Could not get types from assembly '{0}'", assembly.FullName));
                result = false;
            }

            if (DebugEnabled || result)
            {
                _logger.WriteVerbose(string.Format("Assembly {0}, found any class with {1}: {2}", assembly.FullName,
                    string.Join(" | ", _typesToFind.Select(type => type.FullName)), result));
            }

            return result;
        }

        public bool TryIsTypeTestFixture(Type typeToInvestigate)
        {
            if (typeToInvestigate == null)
            {
                throw new ArgumentNullException("typeToInvestigate");
            }

            try
            {
                var toInvestigate = typeToInvestigate.FullName;
                _logger.WriteDebug(string.Format("Testing type '{0}'", toInvestigate));
                var any = IsTypeUnitTestFixture(typeToInvestigate);

                return any;
            }
            catch (Exception ex)
            {
                _logger.WriteDebug(string.Format("Failed to determine if type {0} is {1} {2}",
                    typeToInvestigate.AssemblyQualifiedName,
                    string.Join(" | ", _typesToFind.Select(type => type.FullName)), ex.Message));
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
                        var any = _typesToFind.Any(
                            type =>
                                field.FieldType.FullName != null && type.FullName == field.FieldType.FullName);

                        if (field.FieldType.IsGenericType)
                        {
                            const string genericPartSeparator = "`";
                            var fieldIndex = field.FieldType.FullName.IndexOf(genericPartSeparator,
                                StringComparison.InvariantCultureIgnoreCase);

                            var fieldName = field.FieldType.FullName.Substring(0, fieldIndex);

                            return _typesToFind.Any(
                                type =>
                                {
                                    var typePosition = type.FullName.IndexOf(genericPartSeparator,
                                        StringComparison.InvariantCultureIgnoreCase);

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

                if (DebugEnabled)
                {
                    _logger.WriteVerbose(string.Format("Found {0} types in assembly '{1}'", count, assembly.Location));
                }

                return assembly;
            }
            catch
            {
                return null;
            }
        }
    }
}