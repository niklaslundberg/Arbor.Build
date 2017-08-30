using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    [Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_checking_is_blacklisted_for_a_blacklisted_file
    {
        private static readonly PathLookupSpecification path_lookup_specification =
            DefaultPaths.DefaultPathLookupSpecification.WithIgnoredFileNameParts(new[] { ".vshost." });

        private static bool result;

        private Establish context = () => { };

        private Because of = () =>
        {
            result = path_lookup_specification.IsFileBlackListed(@"C:\test.vshost.exe",
                allowNonExistingFiles: true,
                logger: new ConsoleLogger());
        };

        private It should_be_true = () => result.ShouldBeTrue();
    }
}
