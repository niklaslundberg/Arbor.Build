using System;
using System.Linq;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Bootstrapper
{
    public class BootstrapStartOptions
    {
        public BootstrapStartOptions(
            string baseDir = null,
            bool? prereleaseEnabled = null,
            string branchName = null,
            bool downloadOnly = false)
        {
            BaseDir = baseDir;
            PrereleaseEnabled = prereleaseEnabled;
            BranchName = branchName;
            DownloadOnly = downloadOnly;
        }

        public bool? PrereleaseEnabled { get; }

        public string BaseDir { get; }

        public string BranchName { get; }

        public bool DownloadOnly { get; }

        public static BootstrapStartOptions Parse([NotNull] string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            bool downloadOnly = args.Any(arg => arg.Equals("--download-only", StringComparison.OrdinalIgnoreCase));

            return new BootstrapStartOptions(downloadOnly: downloadOnly);
        }
    }
}
