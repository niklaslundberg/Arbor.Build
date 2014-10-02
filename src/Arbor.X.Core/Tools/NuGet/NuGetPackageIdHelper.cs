using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core.Tools.Git;
using NUnit.Framework;

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

            var invalidCharacters = new List<string> {"<", "@"};

            var trimmedName = invalidCharacters.Aggregate(nugetPackageId, (current, invalidCharacter) => current.Replace(invalidCharacter, ""));

            return trimmedName;
        }
    }
}