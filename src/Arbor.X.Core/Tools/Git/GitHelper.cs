using System;
using System.IO;

namespace Arbor.X.Core.Tools.Git
{
    public static class GitHelper
    {
        public static string GetGitExePath()
        {
            var gitExePath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin",
                    "git.exe");

            var exePath = !File.Exists(gitExePath) ? string.Empty : gitExePath;

            return exePath;
        }
    }
}