using System;

namespace Arbor.X.Core
{
    public static class GitBranchNameExtensions
    {
        public static Maybe<string> GetBranchName(this string potentialBranchName)
        {
            if (string.IsNullOrWhiteSpace(potentialBranchName))
            {
                return Maybe<string>.Empty();
            }

            string branchName;

            string name = potentialBranchName.Trim('#').Trim();

            string prefix = "On branch ";

            if (name.Trim().StartsWith("HEAD detached"))
            {
                return Maybe<string>.Empty();
            }

            if (name.Trim().StartsWith("HEAD (no branch)"))
            {
                return Maybe<string>.Empty();
            }

            if (name.Trim().Equals("HEAD"))
            {
                return Maybe<string>.Empty();
            }

            int indexOfBranchSeparator = name.IndexOf("...", StringComparison.OrdinalIgnoreCase);

            if (indexOfBranchSeparator >= 0)
            {
                branchName = name.Substring(0, indexOfBranchSeparator);

                return new Maybe<string>(branchName);
            }

            if (name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
            {
                branchName = name.Substring(prefix.Length);

                return branchName;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Maybe<string>.Empty();
            }

            branchName = name;

            return new Maybe<string>(branchName);
        }
    }
}
