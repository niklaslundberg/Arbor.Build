using System;
using System.Linq;

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
    }
}