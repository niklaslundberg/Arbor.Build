
using System.IO;
using Arbor.X.Core.IO;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    [Subject(typeof (Core.IO.PathExtensions))]
    public class when_checking_file_is_blacklisted_without_root_dir
    {
        static bool isBlackListed;
        static PathLookupSpecification specification;

        Establish context = () =>
        {
            new DirectoryInfo(@"C:\Temp\root\afolder").EnsureExists();
            using (File.Create(@"C:\Temp\root\afile.txt"))
            {
            }
            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of = () => { isBlackListed = specification.IsFileBlackListed(@"C:\Temp\root\afile.txt"); };
        It should_return_true = () => isBlackListed.ShouldBeTrue();
    }
}