using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.Defensive.Collections;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;
using Arbor.X.Tests.Integration.Tests.MSpec;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests
{
    [Tags(Arbor.X.Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_listing_nuget_package_configuration_files
    {
        private static readonly PathLookupSpecification pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;
        private static DirectoryInfo rootDirectory;
        private static IReadOnlyCollection<FileInfo> packageConfigFiles;

        private Establish context = () =>
        {
            string rootPath = VcsTestPathHelper.FindVcsRootPath();

            rootDirectory = new DirectoryInfo(rootPath);
        };

        private Because of = () =>
        {
            packageConfigFiles = rootDirectory.EnumerateFiles("packages.config", SearchOption.AllDirectories)
                .Where(file => !pathLookupSpecification.IsFileBlackListed(file.FullName, rootDir: rootDirectory.FullName))
                .ToReadOnlyCollection();

            packageConfigFiles
                .Select(file => file.FullName).ToList()
                .ForEach(Console.WriteLine);
        };

        private It should_not_be_empty = () => packageConfigFiles.ShouldNotBeEmpty();

        private It should_not_contained_default_blacklisted =
            () => packageConfigFiles.ShouldEachConformTo(file => !file.FullName.Contains("_Dummy"));
    }
}
