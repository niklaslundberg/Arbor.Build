using System;
using Arbor.Build.Core.IO;
using Shouldly;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions;

public class IsAllowedTests
{
    [Fact]
    public void AllowedCaseInsensitive()
    {
        using var fileSystem = new MemoryFileSystem();
        PathLookupSpecification pathLookupSpecification = new PathLookupSpecification();
        const string testSourcePath = "/test/allowed/source";
        fileSystem.CreateDirectory(testSourcePath);
        DirectoryEntry sourceDir = fileSystem.GetDirectoryEntry(testSourcePath);
        DirectoryEntry rootDir = fileSystem.GetDirectoryEntry("/");
        (bool isAllowed, string _) = pathLookupSpecification.IsAllowed(sourceDir, rootDir);

        isAllowed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("notallowed")]
    [InlineData("notAllowed")]
    public void NotAllowedCaseInsensitive(string notAllowed)
    {
        using var fileSystem = new MemoryFileSystem();
        PathLookupSpecification pathLookupSpecification = new PathLookupSpecification(ignoredDirectorySegmentParts: [notAllowed]);
        const string testSourcePath = "/test/notAllowed/source";
        fileSystem.CreateDirectory(testSourcePath);
        DirectoryEntry sourceDir = fileSystem.GetDirectoryEntry(testSourcePath);
        DirectoryEntry rootDir = fileSystem.GetDirectoryEntry("/");
        (bool isAllowed, string _) = pathLookupSpecification.IsAllowed(sourceDir, rootDir);

        isAllowed.ShouldBeFalse();
    }

    [Fact]
    public void NotAllowedCaseSensitive()
    {
        using var fileSystem = new MemoryFileSystem();
        PathLookupSpecification pathLookupSpecification = new PathLookupSpecification(ignoredDirectorySegmentParts: ["notAllowed"], stringComparison: StringComparison.Ordinal);
        const string testSourcePath = "/test/notAllowed/source";
        fileSystem.CreateDirectory(testSourcePath);
        DirectoryEntry sourceDir = fileSystem.GetDirectoryEntry(testSourcePath);
        DirectoryEntry rootDir = fileSystem.GetDirectoryEntry("/");
        (bool isAllowed, string _) = pathLookupSpecification.IsAllowed(sourceDir, rootDir);

        isAllowed.ShouldBeFalse();
    }

    [Fact]
    public void AllowedNotMatchingCaseSensitive()
    {
        using var fileSystem = new MemoryFileSystem();
        PathLookupSpecification pathLookupSpecification =
            new PathLookupSpecification(ignoredDirectorySegmentParts: ["notAllowed"], stringComparison: StringComparison.Ordinal);
        const string testSourcePath = "/test/notallowed/source";
        fileSystem.CreateDirectory(testSourcePath);
        DirectoryEntry sourceDir = fileSystem.GetDirectoryEntry(testSourcePath);
        DirectoryEntry rootDir = fileSystem.GetDirectoryEntry("/");
        (bool isAllowed, string _) = pathLookupSpecification.IsAllowed(sourceDir, rootDir);

        isAllowed.ShouldBeTrue();
    }
}