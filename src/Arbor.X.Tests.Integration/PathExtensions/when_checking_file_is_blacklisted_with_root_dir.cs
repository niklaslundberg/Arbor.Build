using System;
using System.IO;
using Arbor.X.Core.IO;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    [Subject(typeof(Core.IO.PathExtensions))]
    public class when_checking_file_is_blacklisted_with_root_dir
    {
        static bool isBlackListed;

        static PathLookupSpecification specification;

        static string root;

        Cleanup after = () => { new DirectoryInfo(root).DeleteIfExists(true); };

        Establish context = () =>
        {
            root = $@"C:\Temp\root-{Guid.NewGuid()}";

            new DirectoryInfo(Path.Combine(root, "afolder")).EnsureExists();
            using (File.Create(Path.Combine(root, "afile.txt")))
            {
            }

            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of =
            () => { isBlackListed = specification.IsFileBlackListed($@"{root}\afile.txt", @"C:\Temp\root").Item1; };

        It should_return_false = () => isBlackListed.ShouldBeFalse();
    }
}
