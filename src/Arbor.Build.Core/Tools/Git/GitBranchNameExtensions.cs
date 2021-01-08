using System;

namespace Arbor.Build.Core.Tools.Git
{
    public static class GitBranchNameExtensions
    {
        public static string? GetBranchName(this string potentialBranchName)
        {
            if (string.IsNullOrWhiteSpace(potentialBranchName))
            {
                return default;
            }

            string branchName;

            string name = potentialBranchName.Trim('#').Trim();

            const string prefix = "On branch ";

            if (name.Trim().StartsWith("HEAD detached", StringComparison.Ordinal))
            {
                return default;
            }

            if (name.Trim().StartsWith("HEAD (no branch)", StringComparison.Ordinal))
            {
                return default;
            }

            if (name.Trim().Equals("HEAD", StringComparison.Ordinal))
            {
                return default;
            }

            int indexOfBranchSeparator = name.IndexOf("...", StringComparison.OrdinalIgnoreCase);

            if (indexOfBranchSeparator >= 0)
            {
                branchName = name.Substring(0, indexOfBranchSeparator);

                return branchName;
            }

            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                branchName = name[prefix.Length..];

                return branchName;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return default;
            }

            branchName = name;

            return branchName;
        }
    }
}
