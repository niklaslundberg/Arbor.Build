using System.IO;
using Arbor.Build.Core.IO;
using Machine.Specifications;

namespace Arbor.Build.Tests.Integration.PathExtensions
{
    [Subject(typeof(Core.IO.PathExtensions))]
    public class when_checking_is_blacklisted_without_root_dir
    {
        static bool isBlackListed;
        static PathLookupSpecification specification;
        static DirectoryInfo tempDir;
        Cleanup after = () => tempDir.DeleteIfExists(true);

        Establish context = () =>
        {
            tempDir = new DirectoryInfo(@"C:\Temp\root\afolder").EnsureExists();
            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of = () => isBlackListed = specification.IsBlackListed(@"C:\Temp\root\afolder").Item1;
        It should_return_true = () => isBlackListed.ShouldBeTrue();
    }
}
