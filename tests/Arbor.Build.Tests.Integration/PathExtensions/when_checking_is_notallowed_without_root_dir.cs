using System.IO;
using Arbor.Build.Core.IO;
using Arbor.FS;
using Machine.Specifications;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions;

[Subject(typeof(Core.IO.PathExtensions))]
public class when_checking_is_notallowed_without_root_dir
{
    static bool isNotAllowed;
    static PathLookupSpecification specification;
    static DirectoryEntry tempDir;
    Cleanup after = () =>
    {
        tempDir.DeleteIfExists();
        fs.Dispose();
    };

    Establish context = () =>
    {
        fs = new PhysicalFileSystem();
        // Use a "temp" segment in the path so DefaultPathLookupSpecification excludes it
        var tempPath = UPath.Combine(Path.GetTempPath().ParseAsPath(), "temp", $"root_{System.Guid.NewGuid():N}", "afolder");
        tempDir = new DirectoryEntry(fs, tempPath).EnsureExists();
        specification = DefaultPaths.DefaultPathLookupSpecification;
    };

    Because of = () => isNotAllowed = specification.IsNotAllowed(fs.GetDirectoryEntry(tempDir.Path)).Item1;
    It should_return_true = () => isNotAllowed.ShouldBeTrue();
    static IFileSystem fs;
}