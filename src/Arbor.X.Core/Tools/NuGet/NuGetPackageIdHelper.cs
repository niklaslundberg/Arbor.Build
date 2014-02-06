using System;
using Arbor.X.Core.Tools.Git;

namespace Arbor.X.Core.Tools.NuGet
{
    public static class NuGetPackageIdHelper
    {
        public static string CreateNugetPackageId(string basePackageId, bool isReleaseBuild, string branchName)
        {
            if (string.IsNullOrWhiteSpace(basePackageId))
            {
                throw new ArgumentNullException("basePackageId");
            }

            var branch = new BranchName(branchName);

            if (isReleaseBuild || !branch.IsFeatureBranch())
            {
                return basePackageId;
            }

            var normalizedBranchName = branch.Normalize();

            var nugetPackageId = string.Format("{0}-{1}", basePackageId, normalizedBranchName);
            
            return nugetPackageId;
        }
    }
}