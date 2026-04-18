using System.IO;
using Arbor.FS;
using Xunit;

namespace Arbor.Build.Tests.Unit;

public class SourceRootPathResolutionTests
{
    [Fact(DisplayName = "Given a relative path '.', Path.GetFullPath resolves it to an absolute path")]
    public void RelativeDotResolvesToAbsolutePath()
    {
        string resolved = Path.GetFullPath(".");

        Assert.True(Path.IsPathFullyQualified(resolved), $"Expected absolute path but got: {resolved}");
    }

    [Fact(DisplayName = "Given a relative path '.', it can be parsed as a UPath without throwing")]
    public void RelativeDotCanBeParsedAsUPath()
    {
        string resolved = Path.GetFullPath(".");

        var upath = resolved.ParseAsPath();

        Assert.True(upath.IsAbsolute, $"Expected absolute UPath but got: {upath}");
    }

    [Theory(DisplayName = "Given various relative paths, Path.GetFullPath resolves them to absolute paths")]
    [InlineData(".")]
    [InlineData("./artifacts")]
    [InlineData("src")]
    public void RelativePathsResolveToAbsolutePaths(string relativePath)
    {
        string resolved = Path.GetFullPath(relativePath);

        Assert.True(Path.IsPathFullyQualified(resolved), $"Expected absolute path for '{relativePath}' but got: {resolved}");
    }
}
