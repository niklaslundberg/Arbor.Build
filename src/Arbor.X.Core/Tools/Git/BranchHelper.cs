using System;
using System.Linq;
using NUnit.Framework;

namespace Arbor.X.Core.Tools.Git
{
    public static class BranchHelper
    {
        public static bool IsFeatureBranch(this BranchName branchName)
        {
            var nonFeatureBranchNames = new[] { "dev", "develop", "master" };

            var isAStandardBranch =
                nonFeatureBranchNames.Any(name => branchName.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            return !isAStandardBranch;
        }

        public static bool IsDevelopBranch(this BranchName branchName)
        {
            var developBranchNames = new[] { "dev", "develop"};

            bool isDevelopBranch =
                developBranchNames.Any(name => name.Equals(branchName.Name, StringComparison.InvariantCultureIgnoreCase));

            return isDevelopBranch;
        }

        public static bool IsProductionBranch(this BranchName branchName)
        {
            var isProductionBranch = branchName.Name.Equals("master", StringComparison.InvariantCultureIgnoreCase) ||
                    branchName.Name.IndexOf("release", StringComparison.InvariantCultureIgnoreCase) >= 0;

            return isProductionBranch;
        }
    }
}