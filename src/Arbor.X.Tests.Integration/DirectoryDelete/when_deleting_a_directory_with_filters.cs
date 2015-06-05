using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.X.Core.Logging;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.DirectoryDelete
{
    [Subject(typeof (Core.Tools.DirectoryDelete))]
    [Tags(Arbor.X.Core.Tools.Testing.MSpecInternalConstants.RecursiveArborXTest)]
    public class when_deleting_a_directory_with_filters
    {
        static string tempDir;
        static string[] expectedDirectories;
        static string[] expectedFiles;
        static Core.Tools.DirectoryDelete directoryDelete;

        Cleanup after = () =>
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir);
            }
        };

        Establish context = () =>
        {
            tempDir = Path.Combine(Path.GetTempPath(), "Arbor.X.DeleteDirs");

            var directoryInfo = new DirectoryInfo(tempDir);

            DirectoryInfo a = directoryInfo.CreateSubdirectory("A");
            DirectoryInfo b = directoryInfo.CreateSubdirectory("B");
            DirectoryInfo c = directoryInfo.CreateSubdirectory("C");
            DirectoryInfo d = directoryInfo.CreateSubdirectory("D");
            DirectoryInfo e = directoryInfo.CreateSubdirectory("E");

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

            expectedDirectories = new List<string> {"A","B","A1","A12", "B2"}.ToArray();
            expectedFiles = new List<string> {"app_offline.htm"}.ToArray();

            directoryDelete = new Core.Tools.DirectoryDelete(new[] {"A12"}, expectedFiles, new ConsoleLogger());
        };

        Because of = () => directoryDelete.Delete(tempDir);

        It should_delete_non_filtered_directories = () =>
        {
            string[] enumerateDirectories =
                Directory.EnumerateDirectories(tempDir, "*.*", SearchOption.AllDirectories).ToArray();

            foreach (string enumerateDirectory in enumerateDirectories)
            {
                Console.WriteLine(enumerateDirectory);
            }

            string[] existing = enumerateDirectories.Select(dir => new DirectoryInfo(dir).Name).ToArray();
            existing.ShouldContain(expectedDirectories);
        };

        It should_keep_correct_directory_count = () => Directory.EnumerateDirectories(tempDir, "*.*", SearchOption.AllDirectories)
            .Count()
            .ShouldEqual(expectedDirectories.Length);

        It should_keep_correct_files_count = () => Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories)
            .Count()
            .ShouldEqual(expectedFiles.Length);

        It should_delete_non_filtered_files = () =>
        {
            string[] filesPaths = Directory.EnumerateFiles(tempDir, "*.*", SearchOption.AllDirectories).ToArray();

            foreach (string filePath in filesPaths)
            {
                Console.WriteLine(filePath);
            }

            string[] existingFilePaths = filesPaths.Select(file => new FileInfo(file).Name).ToArray();
            existingFilePaths.ShouldContain(expectedFiles);
        };
    }
}