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
                return name[..indexOfBranchSeparator];
            }

            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return name[prefix.Length..];
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return default;
            }

            return name;
        }
    }
}
