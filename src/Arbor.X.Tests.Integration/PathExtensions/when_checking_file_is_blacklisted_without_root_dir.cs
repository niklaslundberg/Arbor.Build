using System.IO;
using Arbor.Build.Core.IO;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.PathExtensions
{
    [Subject(typeof(Core.IO.PathExtensions))]
    public class when_checking_file_is_blacklisted_without_root_dir
    {
        static bool isBlackListed;
        static PathLookupSpecification specification;
        static DirectoryInfo root;

        static DirectoryInfo rootParent;

        Cleanup after = () =>
        {
            root.DeleteIfExists(true);
            rootParent.DeleteIfExists();
        };

        Establish context = () =>
        {
            root = new DirectoryInfo(@"C:\Temp\root\afolder").EnsureExists();

            rootParent = root.Parent;

            using (File.Create(@"C:\Temp\root\afile.txt"))
            {
            }

            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of = () => isBlackListed = specification.IsFileExcluded(@"C:\Temp\root\afile.txt").Item1;

        It should_return_true = () => isBlackListed.ShouldBeTrue();
    }
}
