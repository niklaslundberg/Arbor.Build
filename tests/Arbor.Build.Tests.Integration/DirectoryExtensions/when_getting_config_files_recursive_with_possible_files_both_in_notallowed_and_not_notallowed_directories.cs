﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbor.Build.Core.IO;
using Arbor.FS;
using Machine.Specifications;
using Machine.Specifications.Model;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.DirectoryExtensions
{
    [Subject(typeof(Subject))]
    public class
        when_getting_config_files_recursive_with_possible_files_both_in_notallowed_and_not_notallowed_directories
    {
        static DirectoryEntry baseDir;
        static IReadOnlyCollection<string> files;

        Cleanup after = () => baseDir.DeleteIfExists();

        Establish context = () =>
        {
            fs = new WindowsFs(new PhysicalFileSystem());
            baseDir =
                new DirectoryEntry(fs,Path.Combine(Path.GetTempPath(),
                    $"{DefaultPaths.TempPathPrefix}_DirectoryExtensions_{Guid.NewGuid()}"));
            baseDir.EnsureExists();

            DirectoryEntry a = baseDir.CreateSubdirectory("A");
            DirectoryEntry bower = baseDir.CreateSubdirectory("bower_components");
            DirectoryEntry c = baseDir.CreateSubdirectory("C");

            DirectoryEntry e = c.CreateSubdirectory("e");

            DirectoryEntry nodeModules = bower.CreateSubdirectory("node_modules");

            using (File.Create(Path.Combine(nodeModules.FullName, "node.config")))
            {
            }

            using (File.Create(Path.Combine(nodeModules.FullName, "node.debug.config")))
            {
            }

            using (File.Create(Path.Combine(bower.FullName, "bower.config")))
            {
            }

            using (File.Create(Path.Combine(bower.FullName, "bower.debug.config")))
            {
            }

            using (File.Create(Path.Combine(a.FullName, "atest.config")))
            {
            }

            using (File.Create(Path.Combine(a.FullName, "atest.debug.config")))
            {
            }

            using (File.Create(Path.Combine(e.FullName, "etest.config")))
            {
            }

            using (File.Create(Path.Combine(e.FullName, "etest.debug.config")))
            {
            }
        };

        Because of = () =>
        {
            files = baseDir.GetFilesRecursive(new List<string> { ".config" },
                    DefaultPaths.DefaultPathLookupSpecification,
                    baseDir.FullName)
                .Select(s => s.Name)
                .ToList();
        };

        It should_contain_not_notallowed_files =
            () => files.ShouldContain("atest.config", "atest.debug.config", "etest.debug.config", "etest.config");

        It should_containt_correct_file_count = () => files.Count.ShouldEqual(4);

        It should_not_contain_notallowed_files =
            () => files.ShouldNotContain("bower.config", "bower.debug.config", "node.debug.config", "node.config");

        static WindowsFs fs;
    }
}
