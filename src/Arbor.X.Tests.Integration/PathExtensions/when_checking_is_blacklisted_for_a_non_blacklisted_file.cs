using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.PathExtensions
{
    [Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_checking_is_blacklisted_for_a_non_blacklisted_file
    {
        static readonly PathLookupSpecification path_lookup_specification =
            DefaultPaths.DefaultPathLookupSpecification.WithIgnoredFileNameParts(new[] { "" });

        static bool result;

        Establish context = () => { };

        Because of = () =>
        {
            result = path_lookup_specification.IsFileBlackListed(@"C:\anyrandomfile.txt",
                allowNonExistingFiles: true,
                logger: new ConsoleLogger()).Item1;
        };

        It should_be_true = () => result.ShouldBeFalse();
    }
}
