using System;
using System.IO;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Testing;
using Arbor.FS;
using Machine.Specifications;
using Serilog.Core;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions;

[Tags(MSpecInternalConstants.RecursiveArborXTest)]
public class when_checking_is_notallowed_for_a_non_notallowed_file
{
    static readonly PathLookupSpecification path_lookup_specification =
        DefaultPaths.DefaultPathLookupSpecification.WithIgnoredFileNameParts(new[] {string.Empty});

    static bool result;

    static IFileSystem fs;
    static UPath filePath;
    Cleanup after = () => fs.Dispose();

    Establish context = () =>
    {
        fs = new MemoryFileSystem();
        filePath = $@"/anyrandomfile{Guid.NewGuid()}.txt".ParseAsPath();
        using var _ = fs.OpenFile(filePath, FileMode.CreateNew,
            FileAccess.Write);
    };

    Because of = () =>
    {
        result = path_lookup_specification.IsFileExcluded(fs.GetFileEntry(filePath),
            allowNonExistingFiles: true,
            logger: Logger.None).Item1;
    };

    It should_be_true = () => result.ShouldBeFalse();
}