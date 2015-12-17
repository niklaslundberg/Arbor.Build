using Alphaleonis.Win32.Filesystem;
using Arbor.X.Core.IO;
using Arbor.X.Core.Tools;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    [Subject(typeof (Core.IO.PathExtensions))]
    public class when_checking_is_blacklisted_without_root_dir
    {
        static bool isBlackListed;
        static PathLookupSpecification specification;

        Establish context = () =>
        {
            new DirectoryInfo(@"C:\Temp\root\afolder").EnsureExists();
            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of = () => { isBlackListed = specification.IsBlackListed(@"C:\Temp\root\afolder"); };
        It should_return_true = () => isBlackListed.ShouldBeTrue();
    }
}