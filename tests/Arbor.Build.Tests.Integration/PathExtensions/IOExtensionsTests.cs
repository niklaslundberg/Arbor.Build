using System;
using System.Collections.Generic;
using System.IO;
using Arbor.FS;
using FluentAssertions;
using Machine.Specifications;
using Xunit;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.PathExtensions;

public class IOExtensionsTests
{
    public static IEnumerable<object[]> GetFileSystems()
    {
        foreach (var fileSystem in EnumerateFilesSystem())
        {
            yield return [fileSystem];
        }
    }

    private static IEnumerable<IFileSystem> EnumerateFilesSystem()
    {
        yield return new MemoryFileSystem();
        yield return new PhysicalFileSystem();
    }

    public static IEnumerable<string> GetPaths()
    {

        var data = new List<string>
        {
            "/mnt/c/temp/afile.txt",
            "/mnt/c/temp",
            "/mnt/c/temp/",
            @"C:\temp\afile.txt",
            @"C:\temp",
            @"C:\temp\"
        };

        foreach (string path in data)
        {
            yield return path;
        }
    }

    public static IEnumerable<object[]> GetPathData()
    {

        foreach (string path in GetPaths())
        {
            yield return [path];
        }

    }

    [MemberData(nameof(GetFileSystems))]
    [Theory]
    public void EnsureExistsWhenExistsShouldNotThrowException(IFileSystem fs)
    {
        Exception? exception = null;
        using (fs)
        {
            string systemPath = Path.GetTempPath();

            var tempPath = systemPath.ParseAsPath();

            var path = UPath.Combine(tempPath, "123" + Guid.NewGuid());

            fs.CreateDirectory(path);
            try
            {
                fs.GetDirectoryEntry(path).EnsureExists();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                fs.DeleteDirectory(path, true);
                fs.Dispose();
            }
        }

        exception.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(GetFileSystems))]
    public void DeleteFileIfExistsShouldNotThrowIfNotExists(IFileSystem fileSystem)
    {
        fileSystem.CreateDirectory("/mnt/c/temp");
        var file = new FileEntry(fileSystem, "/mnt/c/temp/exampleFile.txt");

        file.DeleteIfExists().Should().BeTrue();
    }

}