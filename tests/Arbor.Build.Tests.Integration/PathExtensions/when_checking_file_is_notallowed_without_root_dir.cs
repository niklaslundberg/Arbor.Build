using System.IO;
using Arbor.Build.Core.IO;
using Arbor.FS;
using Machine.Specifications;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions;

[Subject(typeof(Core.IO.PathExtensions))]
public class when_checking_file_is_notallowed_without_root_dir
{
    static bool isNotAllowed;
    static PathLookupSpecification specification;
    static DirectoryEntry root;

    static DirectoryEntry rootParent;

    Cleanup after = () =>
    {
        root.DeleteIfExists();
        rootParent.DeleteIfExists();
        fs.Dispose();
    };

    static IFileSystem fs;

    Establish context = () =>
    {
        fs = new PhysicalFileSystem();
        // Use a "temp" segment so DefaultPathLookupSpecification excludes it (without root)
        var basePath = UPath.Combine(Path.GetTempPath().ParseAsPath(), "temp", $"root_{System.Guid.NewGuid():N}");
        var rootPath = UPath.Combine(basePath, "afolder");
        root = new DirectoryEntry(fs, rootPath).EnsureExists();

        rootParent = root.Parent!;

        using (fs.OpenFile(UPath.Combine(basePath, "afile.txt"), FileMode.Create, FileAccess.Write))
        {
        }

        specification = DefaultPaths.DefaultPathLookupSpecification;
    };

    Because of = () => isNotAllowed = specification.IsFileExcluded(fs.GetFileEntry(UPath.Combine(rootParent.Path, "afile.txt"))).Item1;

    It should_return_true = () => isNotAllowed.ShouldBeTrue();
}