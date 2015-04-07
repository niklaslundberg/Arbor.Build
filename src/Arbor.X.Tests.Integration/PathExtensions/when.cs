using Arbor.X.Core.IO;
using Arbor.X.Core.Tools;
using Machine.Specifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    public class when_checking_is_blacklisted_without_root_dir
    {
        Establish context = () =>
        {
            new DirectoryInfo(@"C:\Temp\root\afolder").EnsureExists();
            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of = () => {
            isBlackListed = specification.IsBlackListed(@"C:\Temp\root\afolder");
        };

        It should_return_true = () => isBlackListed.ShouldBeTrue();

        static bool isBlackListed;
        private static PathLookupSpecification specification;
    }

    public class when_checking_is_blacklisted_with_root_dir
    {
        Establish context = () =>
        {
            new DirectoryInfo(@"C:\Temp\root\afolder").EnsureExists();
            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of = () => {
            isBlackListed = specification.IsBlackListed(@"C:\Temp\root\afolder", @"C:\Temp\root");
        };

        It should_return_false = () => isBlackListed.ShouldBeFalse();

        static bool isBlackListed;
        private static PathLookupSpecification specification;
    }
}
