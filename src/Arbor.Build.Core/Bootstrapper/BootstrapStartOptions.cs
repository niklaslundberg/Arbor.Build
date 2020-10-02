using System;
using System.Linq;
using JetBrains.Annotations;
using Zio;

namespace Arbor.Build.Core.Bootstrapper
{
    public class BootstrapStartOptions
    {
        public const string DownloadOnlyCliParameter = "--download-only";
        public const string ArborBuildExeCliParameter = "-arborBuildExe=";

        public BootstrapStartOptions(string[] args,
            DirectoryEntry? baseDir = null,
            bool? preReleaseEnabled = null,
            string? branchName = null,
            bool downloadOnly = false,
            string? arborBuildExePath = default)
        {
            Args = args;
            BaseDir = baseDir;
            PreReleaseEnabled = preReleaseEnabled;
            BranchName = branchName;
            DownloadOnly = downloadOnly;
            ArborBuildExePath = arborBuildExePath;
        }

        public bool? PreReleaseEnabled { get; }

        public string[] Args { get; }

        public DirectoryEntry? BaseDir { get; }

        public string? BranchName { get; }

        public bool DownloadOnly { get; }

        public string? ArborBuildExePath { get; }

        public static BootstrapStartOptions Parse([NotNull] string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            bool downloadOnly = args.Any(arg => arg.Equals(DownloadOnlyCliParameter, StringComparison.OrdinalIgnoreCase));

            string? arborBuildExePath = args.FirstOrDefault(arg => arg.StartsWith(ArborBuildExeCliParameter, StringComparison.OrdinalIgnoreCase))?.Split("=").Skip(1).FirstOrDefault();

            return new BootstrapStartOptions(args, downloadOnly: downloadOnly, arborBuildExePath: arborBuildExePath);
        }
    }
}
