using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Git
{
    public static class GitHelper
    {
        public static string GetGitExePath([NotNull] ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var gitExeLocations = new List<string>
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Git",
                    "bin",
                    "git.exe"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Git",
                    "bin",
                    "git.exe")
            };

            string programFilesX64 = Environment.GetEnvironmentVariable("ProgramW6432");

            if (!string.IsNullOrWhiteSpace(programFilesX64))
            {
                string programFilesX64FullPath = Path.Combine(
                    programFilesX64,
                    "Git",
                    "bin",
                    "git.exe");

                gitExeLocations.Insert(0, programFilesX64FullPath);
            }

            string exePath = gitExeLocations.FirstOrDefault(
                                 location =>
                                 {
                                     bool exists = File.Exists(location);

                                     logger.Debug("Testing Git exe path '{Location}', exists: {Exists}",
                                         location,
                                         exists);

                                     return exists;
                                 }) ?? string.Empty;

            return exePath;
        }
    }
}
