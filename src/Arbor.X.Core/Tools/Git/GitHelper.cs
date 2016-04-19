using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arbor.X.Core.Tools.Git
{
    public static class GitHelper
    {
        public static string GetGitExePath()
        {
            var gitExeLocations = new List<string>
                                      {
                                          Path.Combine(
                                              Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                              "Git",
                                              "bin",
                                              "git.exe"),

                                          Path.Combine(
                                              Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                              "Git",
                                              "bin",
                                              "git.exe")
                                      };


            string exePath = gitExeLocations.FirstOrDefault(File.Exists) ?? "";


            return exePath;
        }
    }
}
