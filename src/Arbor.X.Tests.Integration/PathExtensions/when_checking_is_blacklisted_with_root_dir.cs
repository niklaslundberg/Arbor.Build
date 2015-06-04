using Alphaleonis.Win32.Filesystem;
using Arbor.X.Core.IO;
using Arbor.X.Core.Tools;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    [Subject(typeof (Core.Tools.PathExtensions))]
    public class when_checking_is_blacklisted_with_root_dir
    {
        static bool isBlackListed;
        static PathLookupSpecification specification;

        Establish context = () =>
        {
            new DirectoryInfo(@"C:\Temp\root\afolder").EnsureExists();
            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of = () => { isBlackListed = specification.IsBlackListed(@"C:\Temp\root\afolder", @"C:\Temp\root"); };
        It should_return_false = () => isBlackListed.ShouldBeFalse();
    }
}