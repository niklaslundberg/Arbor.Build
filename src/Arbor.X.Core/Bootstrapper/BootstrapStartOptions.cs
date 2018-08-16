using System;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Bootstrapper
{
    public class BootstrapStartOptions
    {
        public BootstrapStartOptions(string baseDir = null, bool? prereleaseEnabled = null, string branchName = null)
        {
            BaseDir = baseDir;
            PrereleaseEnabled = prereleaseEnabled;
            BranchName = branchName;
        }

        public bool? PrereleaseEnabled { get; }

        public string BaseDir { get; }

        public string BranchName { get; }

        public static BootstrapStartOptions Parse([NotNull] string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return new BootstrapStartOptions();
        }
    }
}
