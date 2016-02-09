using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core.Tools.Git;

namespace Arbor.X.Core.Tools.NuGet
{
    public static class NuGetPackageIdHelper
    {
        public static string CreateNugetPackageId(string basePackageId, bool isReleaseBuild, string branchName, bool branchNameEnabled)
        {
            if (string.IsNullOrWhiteSpace(basePackageId))
            {
                throw new ArgumentNullException(nameof(basePackageId));
            }

            var branch = new BranchName(branchName);

            if (isReleaseBuild || !branch.IsFeatureBranch())
            {
                if (!branchNameEnabled)
                {
                    return basePackageId;
                }
            }

            return CreateNugetPackageIdWithBranchName(basePackageId, branch);
        }

        static string CreateNugetPackageIdWithBranchName(string basePackageId, BranchName branch)
        {
            var normalizedBranchName = branch.Normalize();

            var nugetPackageId = string.Format("{0}-{1}", basePackageId, normalizedBranchName);

            var invalidCharacters = new List<string> {"<", "@", ">", "|", "?", ":"};

            var trimmedName = invalidCharacters.Aggregate(nugetPackageId,
                (current, invalidCharacter) => current.Replace(invalidCharacter, ""));

            return trimmedName;
        }
    }
}
