using System.IO;
using Arbor.Build.Core.IO;
using Arbor.FS;
using Machine.Specifications;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions;

[Subject(typeof(Core.IO.PathExtensions))]
public class when_checking_is_notallowed_with_root_dir
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
        var basePath = UPath.Combine(Path.GetTempPath().ParseAsPath(), $"ABX_{System.Guid.NewGuid():N}");
        var rootPath = UPath.Combine(basePath, "root");
        var aPath = UPath.Combine(basePath, "root", "afolder");
        fs.CreateDirectory(aPath);

        tempDir = new DirectoryEntry(fs, aPath);
        specification = DefaultPaths.DefaultPathLookupSpecification;
        rootDir = new DirectoryEntry(fs, rootPath);
        sourceDir = fs.GetDirectoryEntry(aPath);
    };

    Because of = () =>
    {
        isNotAllowed = specification.IsNotAllowed(sourceDir, rootDir).Item1;
    };

    It should_return_false = () => isNotAllowed.ShouldBeFalse();
    static DirectoryEntry rootDir;
    static DirectoryEntry sourceDir;
    static IFileSystem fs;
}