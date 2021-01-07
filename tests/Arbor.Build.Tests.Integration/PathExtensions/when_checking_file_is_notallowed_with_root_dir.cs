using System;
using System.IO;
using Arbor.Build.Core.IO;
using Machine.Specifications;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions
{
    [Subject(typeof(Core.IO.PathExtensions))]
    public class when_checking_file_is_notallowed_with_root_dir
    {
        static bool isNotAllowed;

        static PathLookupSpecification specification;

        static DirectoryEntry root;

        static IFileSystem fs;

        Cleanup after = () =>
        {
            root.DeleteIfExists();
            fs.Dispose();
        };

        Establish context = () =>
        {
            fs = new PhysicalFileSystem();

            var tempPath = Path.GetTempPath().ParseAsPath();

            var rootPath = UPath.Combine(tempPath, $"root-{Guid.NewGuid()}");
            fs.CreateDirectory(rootPath);
            root = fs.GetDirectoryEntry(rootPath);

            new DirectoryEntry(fs, UPath.Combine(root.Path, "afolder")).EnsureExists();
            using (fs.OpenFile(UPath.Combine(root.Path, "afile.txt"), FileMode.Create, FileAccess.Write))
            {
            }

            specification = DefaultPaths.DefaultPathLookupSpecification;
        };

        Because of =
            () => isNotAllowed = specification
                .IsFileExcluded(fs.GetFileEntry(UPath.Combine(root.Path, "afile.txt")), root).Item1;

        It should_return_false = () => isNotAllowed.ShouldBeFalse();
    }
}