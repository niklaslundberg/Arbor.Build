

using System.IO;
using Arbor.X.Core.IO;

using Machine.Specifications;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    [Subject(typeof(Core.IO.PathExtensions))]
    public class when_checking_file_is_blacklisted_with_root_dir
    {
        private static bool isBlackListed;

        private static PathLookupSpecification specification;

        private static string root;

        private Cleanup after = () => { new DirectoryInfo(root).DeleteIfExists(recursive: true); };

        private Establish context = () =>
            {
                root = @"C:\Temp\root";

                new DirectoryInfo(Path.Combine(root, "afolder")).EnsureExists();
                using (File.Create(Path.Combine(root, "afile.txt")))
                {
                }

                specification = DefaultPaths.DefaultPathLookupSpecification;
            };

        private Because of =
            () => { isBlackListed = specification.IsFileBlackListed(@"C:\Temp\root\afile.txt", @"C:\Temp\root"); };

        private It should_return_false = () => isBlackListed.ShouldBeFalse();
    }
}
