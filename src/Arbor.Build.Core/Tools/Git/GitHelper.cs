using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Platform;
using Arbor.FS;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Git;

public class GitHelper(IFileSystem fileSystem)
{
    public UPath GetGitExePath(ILogger logger, ISpecialFolders specialFolders, IEnvironmentVariables environmentVariables)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var gitExeLocations = new List<UPath>();

        if (PlatformHelper.IsWindows)
        {
            gitExeLocations.Add(UPath.Combine(
                specialFolders.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).ParseAsPath(),
                "Git",
                "bin",
                "git.exe"));

            var programFilesX64 = environmentVariables.GetEnvironmentVariable("ProgramW6432")?.ParseAsPath();

            if (programFilesX64.HasValue)
            {
                var programFilesX64FullPath = UPath.Combine(
                    programFilesX64.Value,
                    "Git",
                    "bin",
                    "git.exe");

                gitExeLocations.Insert(0, programFilesX64FullPath);
            }
        }
        else if (PlatformHelper.IsLinux || PlatformHelper.IsMacOS)
        {
            gitExeLocations.Add("/usr/bin/git".ParseAsPath());
            gitExeLocations.Add("/usr/local/bin/git".ParseAsPath());
            gitExeLocations.Add("/bin/git".ParseAsPath());
        }

        var exePath = gitExeLocations.FirstOrDefault(
            location =>
            {
                bool exists = fileSystem.FileExists(location);

                logger.Debug("Testing Git exe path '{Location}', exists: {Exists}",
                    fileSystem.ConvertPathToInternal(location),
                    exists);

                return exists;
            });

        return exePath;
    }
}