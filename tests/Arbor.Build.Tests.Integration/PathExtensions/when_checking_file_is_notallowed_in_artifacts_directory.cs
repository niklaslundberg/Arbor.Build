using System;
using System.IO;
using Arbor.Build.Core.IO;
using Arbor.FS;
using Machine.Specifications;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions;

[Subject(typeof(Core.IO.PathExtensions))]
public class when_checking_file_is_notallowed_in_artifacts_directory
{
    static bool isNotAllowed;

    static PathLookupSpecification specification;

    static DirectoryEntry root;

    static IFileSystem fs;

    Cleanup after = () =>
    {
        root.DeleteIfExists();
    };

    Establish context = () =>
    {
        fs = new PhysicalFileSystem();
        var rootPath = $@"C:\Temp\root-{Guid.NewGuid()}".ParseAsPath();
        fs.CreateDirectory(rootPath);
        root = fs.GetDirectoryEntry(rootPath);

        new DirectoryEntry(fs, UPath.Combine(root.Path, "artifacts")).EnsureExists();
        using (fs.OpenFile(UPath.Combine(root.Path, "artifacts", "afile.txt"), FileMode.Create, FileAccess.Write))
        {
        }

        specification = DefaultPaths.DefaultPathLookupSpecification;
    };

    Because of =
        () => isNotAllowed = specification
            .IsFileExcluded(fs.GetFileEntry(UPath.Combine(root.Path, @"artifacts\afile.txt")), root).Item1;

    It should_return_false = () => isNotAllowed.ShouldBeTrue();
}