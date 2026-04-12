using System.IO;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Testing;
using Machine.Specifications;
using Serilog.Core;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions;

[Tags(TestFilterHelper.RecursiveCategoryName)]
public class when_checking_is_notallowed_for_a_notallowed_file
{
    static readonly PathLookupSpecification path_lookup_specification =
        DefaultPaths.DefaultPathLookupSpecification.WithIgnoredFileNameParts([".vshost."]);

    static bool result;

    Establish context = () =>
    {
        fs = new MemoryFileSystem();
        using var _ = fs.OpenFile("/test.vshost.exe", FileMode.Create, FileAccess.Write);
    };

    Because of = () =>
    {
        result = path_lookup_specification.IsFileExcluded(
            fs.GetFileEntry("/test.vshost.exe"),
            allowNonExistingFiles: true,
            logger: Logger.None).Item1;
    };

    It should_be_true = () => result.ShouldBeTrue();

    Cleanup after = () => fs.Dispose();

    static IFileSystem fs;
}