
using System.IO;
using Arbor.X.Core.IO;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    [Subject(typeof (Core.IO.PathExtensions))]
    public class when_checking_is_blacklisted_with_root_dir
    {
        private static bool isBlackListed;
        private static PathLookupSpecification specification;

        private Establish context = () =>
        {
            new DirectoryInfo(@"C:\Temp\root\afolder").EnsureExists();
            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        private Because of = () => { isBlackListed = specification.IsBlackListed(@"C:\Temp\root\afolder", @"C:\Temp\root"); };
        private It should_return_false = () => isBlackListed.ShouldBeFalse();
    }
}