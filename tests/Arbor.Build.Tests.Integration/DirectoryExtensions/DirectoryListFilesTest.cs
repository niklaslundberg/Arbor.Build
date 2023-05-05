using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Arbor.Build.Core.IO;
using Arbor.FS;
using FluentAssertions;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.DirectoryExtensions;

public sealed class DirectoryListFilesTest : IDisposable
{
    readonly IFileSystem _fs;
    readonly DirectoryEntry _tempDirectory;

    public DirectoryListFilesTest()
    {
        _fs = new PhysicalFileSystem();

        _tempDirectory = new DirectoryEntry(_fs,
                UPath.Combine(Path.GetTempPath().ParseAsPath(), Guid.NewGuid().ToString()))
            .EnsureExists();
    }

    public void Dispose()
    {
        _tempDirectory?.DeleteIfExists();
        _fs.Dispose();
    }

    [Fact]
    public void WhenGettingFilesExcludingUserFiles()
    {
        var subDirectoryA = _tempDirectory.CreateSubdirectory("Abc");

        var fileName = UPath.Combine(subDirectoryA.FullName, "def.txt");
        using (_fs.CreateFile(fileName))
        {
        }

        var userFile = UPath.Combine(subDirectoryA.FullName, "def.user");
        using (_fs.CreateFile(userFile))
        {
        }

        subDirectoryA.Parent!.Path.Should().Be(_tempDirectory.Path);

        var files = _tempDirectory.GetFilesWithWithExclusions(new[] {"*.user"})
            .Select(file => file.Path.NormalizePath().FullName)
            .ToImmutableArray();

        Assert.DoesNotContain(userFile.NormalizePath().FullName, files);
        Assert.Single(files);
        Assert.Contains(fileName.NormalizePath().FullName, files, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void WhenGettingFilesExcludingDirectoryPath()
    {
        var subDirectoryA = _tempDirectory.CreateSubdirectory("test/bin/roslyn");

        var aFile = UPath.Combine(subDirectoryA.FullName, "a.txt");
        using (_fs.CreateFile(aFile))
        {
        }

        var bFile = UPath.Combine(subDirectoryA.FullName, "b.user");
        using (_fs.CreateFile(bFile))
        {
        }

        var files = _tempDirectory.GetFilesWithWithExclusions(new[] {"bin\\roslyn\\"})
            .Select(file => file.Path.NormalizePath().FullName)
            .ToImmutableArray();

        Assert.DoesNotContain(aFile.NormalizePath().FullName, files);
        Assert.DoesNotContain(bFile.NormalizePath().FullName, files);
        Assert.Empty(files);
    }
}