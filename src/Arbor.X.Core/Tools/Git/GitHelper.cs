using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arbor.X.Core.Tools.Git
{
    public static class GitHelper
    {
        public static string GetGitExePath()
        {
            var gitExePath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin",
                    "git.exe");

            if (!File.Exists(gitExePath))
            {
                return string.Empty;
            }

            return gitExePath;
        }
    }
}
