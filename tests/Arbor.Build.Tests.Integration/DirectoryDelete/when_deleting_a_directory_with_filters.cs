using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.Build.Core.IO;
using Machine.Specifications;
using Serilog.Core;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.DirectoryDelete
{
    [Subject(typeof(Core.IO.DirectoryDelete))]
    [Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_deleting_a_directory_with_filters
    {
        static DirectoryEntry tempDir;
        static string[] expectedDirectories;
        static string[] expectedFiles;
        static Core.IO.DirectoryDelete directoryDelete;

        Cleanup after = () =>
        {
            tempDir.DeleteIfExists();
            fs.Dispose();
        };

        Establish context = () =>
        {
            fs = new PhysicalFileSystem();
            tempDir = new DirectoryEntry(fs, UPath.Combine(Path.GetTempPath().AsFullPath(),
                $"{DefaultPaths.TempPathPrefix}_DeleteDirs{Guid.NewGuid().ToString().Substring(0, 8)}"));

            tempDir.EnsureExists();

            var a = tempDir.CreateSubdirectory("A");
            var b = tempDir.CreateSubdirectory("B");
            var c = tempDir.CreateSubdirectory("C");
            var d = tempDir.CreateSubdirectory("D");
            var e = tempDir.CreateSubdirectory("E");

            var a1 = a.CreateSubdirectory("A1");
            var a2 = a.CreateSubdirectory("A2");

            var b1 = b.CreateSubdirectory("B1");
            var b2 = b.CreateSubdirectory("B2");
            fs.WriteAllText(UPath.Combine(b1.Path, "B1f.txt"), "B1f");
            fs.WriteAllText(UPath.Combine(b2.Path, "app_offline.htm"), "B2f");

            fs.WriteAllText(UPath.Combine(c.Path, "Cf.txt"), "Cf");
            fs.WriteAllText(UPath.Combine(d.Path, "Df.txt"), "Df");
            fs.WriteAllText(UPath.Combine(e.Path, "Ef.txt"), "Ef");

            a1.CreateSubdirectory("A11");
            a1.CreateSubdirectory("A12");

            a2.CreateSubdirectory("A21");
            a2.CreateSubdirectory("A22");

            expectedDirectories = new List<string> { "A", "B", "A1", "A12", "B2" }.ToArray();
            expectedFiles = new List<string> { "app_offline.htm" }.ToArray();

            directoryDelete = new Core.IO.DirectoryDelete(new[] { "A12" }, expectedFiles, Logger.None);
        };

        Because of = () => directoryDelete.Delete(tempDir);

        It should_delete_non_filtered_directories = () =>
        {
            var enumerateDirectories = new DirectoryEntry(fs, tempDir.Path)
                .EnumerateDirectories("*", SearchOption.AllDirectories).ToArray();

            foreach (var enumerateDirectory in enumerateDirectories)
            {
                Console.WriteLine(enumerateDirectory.FullName);
            }

            string[] existing = enumerateDirectories.Select(dir => dir.Name).ToArray();
            existing.ShouldContain(expectedDirectories);
        };

        It should_delete_non_filtered_files = () =>
        {
            var filesPaths = tempDir.EnumerateFiles( "*", SearchOption.AllDirectories)
                .ToArray();

            foreach (var filePath in filesPaths)
            {
                Console.WriteLine(filePath.FullName);
            }

            string[] existingFilePaths = filesPaths.Select(file => file.Name).ToArray();
            existingFilePaths.ShouldContain(expectedFiles);
        };

        It should_keep_correct_directory_count = () => tempDir
            .EnumerateDirectories("*", SearchOption.AllDirectories)
            .Count()
            .ShouldEqual(expectedDirectories.Length);

        It should_keep_correct_files_count = () => tempDir
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Count()
            .ShouldEqual(expectedFiles.Length);

        static IFileSystem fs;
    }
}
