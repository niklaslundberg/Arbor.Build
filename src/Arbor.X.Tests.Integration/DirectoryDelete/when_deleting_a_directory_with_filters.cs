using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.X.Core.IO;
using Machine.Specifications;
using Serilog.Core;

namespace Arbor.X.Tests.Integration.DirectoryDelete
{
    [Subject(typeof(Core.IO.DirectoryDelete))]
    [Tags(Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_deleting_a_directory_with_filters
    {
        static DirectoryInfo tempDir;
        static string[] expectedDirectories;
        static string[] expectedFiles;
        static Core.IO.DirectoryDelete directoryDelete;

        Cleanup after = () => { tempDir.DeleteIfExists(true); };

        Establish context = () =>
        {
            tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(),
                $"{DefaultPaths.TempPathPrefix}_DeleteDirs{Guid.NewGuid().ToString().Substring(0, 8)}"));

            DirectoryInfo a = tempDir.CreateSubdirectory("A");
            DirectoryInfo b = tempDir.CreateSubdirectory("B");
            DirectoryInfo c = tempDir.CreateSubdirectory("C");
            DirectoryInfo d = tempDir.CreateSubdirectory("D");
            DirectoryInfo e = tempDir.CreateSubdirectory("E");

            DirectoryInfo a1 = a.CreateSubdirectory("A1");
            DirectoryInfo a2 = a.CreateSubdirectory("A2");

            DirectoryInfo b1 = b.CreateSubdirectory("B1");
            DirectoryInfo b2 = b.CreateSubdirectory("B2");

            File.WriteAllText(Path.Combine(b1.FullName, "B1f.txt"), "B1f");
            File.WriteAllText(Path.Combine(b2.FullName, "app_offline.htm"), "B2f");

            File.WriteAllText(Path.Combine(c.FullName, "Cf.txt"), "Cf");
            File.WriteAllText(Path.Combine(d.FullName, "Df.txt"), "Df");
            File.WriteAllText(Path.Combine(e.FullName, "Ef.txt"), "Ef");

            a1.CreateSubdirectory("A11");
            a1.CreateSubdirectory("A12");

            a2.CreateSubdirectory("A21");
            a2.CreateSubdirectory("A22");

            expectedDirectories = new List<string> { "A", "B", "A1", "A12", "B2" }.ToArray();
            expectedFiles = new List<string> { "app_offline.htm" }.ToArray();

            directoryDelete = new Core.IO.DirectoryDelete(new[] { "A12" }, expectedFiles, Logger.None);
        };

        Because of = () => directoryDelete.Delete(tempDir.FullName);

        It should_delete_non_filtered_directories = () =>
        {
            string[] enumerateDirectories =
                Directory.EnumerateDirectories(tempDir.FullName, "*.*", SearchOption.AllDirectories).ToArray();

            foreach (string enumerateDirectory in enumerateDirectories)
            {
                Console.WriteLine(enumerateDirectory);
            }

            string[] existing = enumerateDirectories.Select(dir => new DirectoryInfo(dir).Name).ToArray();
            existing.ShouldContain(expectedDirectories);
        };

        It should_delete_non_filtered_files = () =>
        {
            string[] filesPaths = Directory.EnumerateFiles(tempDir.FullName, "*.*", SearchOption.AllDirectories)
                .ToArray();

            foreach (string filePath in filesPaths)
            {
                Console.WriteLine(filePath);
            }

            string[] existingFilePaths = filesPaths.Select(file => new FileInfo(file).Name).ToArray();
            existingFilePaths.ShouldContain(expectedFiles);
        };

        It should_keep_correct_directory_count = () => Directory
            .EnumerateDirectories(tempDir.FullName, "*.*", SearchOption.AllDirectories)
            .Count()
            .ShouldEqual(expectedDirectories.Length);

        It should_keep_correct_files_count = () => Directory
            .EnumerateFiles(tempDir.FullName, "*.*", SearchOption.AllDirectories)
            .Count()
            .ShouldEqual(expectedFiles.Length);
    }
}
