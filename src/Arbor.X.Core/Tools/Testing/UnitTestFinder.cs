using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Arbor.Aesculus.Core;

namespace Arbor.X.Core.Tools.Testing
{
    public class UnitTestFinder
    {
        readonly IEnumerable<Type> _typesToFind;

        public UnitTestFinder(IEnumerable<Type> typesesToFind, bool debugEnabled = false)
        {
            _typesToFind = typesesToFind;
            DebugEnabled = debugEnabled;
        }

        bool DebugEnabled { get; set; }

        public IReadOnlyCollection<string> GetUnitTestFixtureDlls(DirectoryInfo currentDirectory = null)
        {
            currentDirectory = currentDirectory ?? new DirectoryInfo(VcsPathHelper.FindVcsRootPath());

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
                return new ReadOnlyCollection<string>(new List<string>());
            }

            var dllFiles = currentDirectory.EnumerateFiles("*.dll");

            var assemblies = dllFiles
                .Select(GetAssembly)
                .Where(assembly => assembly != null)
                .Distinct();

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
                Type[] types = assembly.GetExportedTypes();
                var anyType = types.Any(TryIsTypeTestFixture);

                result = anyType;
            }
            catch (Exception)
            {
                result = false;
            }

            if (DebugEnabled || result)
            {
                Debug.WriteLine("Assembly {0}, found any class with {1}: {2}", assembly.FullName,
                    string.Join(" | ", _typesToFind.Select(type => type.FullName)), result);
            }

            return result;
        }

        bool TryIsTypeTestFixture(Type typeToInvestigate)
        {
            try
            {
                var any = IsTypeUnitTestFixture(typeToInvestigate);

                return any;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to determine if type {0} is {1} {2}",
                    typeToInvestigate.AssemblyQualifiedName,
                    string.Join(" | ", _typesToFind.Select(type => type.FullName)), ex.Message);
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

            var hasTestMethod =
                publicInstanceMethods.Any(method => IsCustomAttributeOfExpectedType(method.CustomAttributes));

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
                    Debug.WriteLine("Found {0} types in assembly '{1}'", count, assembly.Location);
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