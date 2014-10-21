using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using Arbor.X.Core.BuildVariables;
using NUnit.Framework;
using Semver;

namespace Arbor.X.Core.Tools.Git
{
    public static class BranchHelper
    {
        public static bool IsFeatureBranch(this BranchName branchName)
        {
            var nonFeatureBranchNames = new[] {"dev", "develop", "master"};

            var isAStandardBranch =
                nonFeatureBranchNames.Any(
                    name => branchName.LogicalName.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));

            return !isAStandardBranch;
        }

        public static bool IsDevelopBranch(this BranchName branchName)
        {
            var developBranchNames = new[] {"dev", "develop"};

            bool isDevelopBranch =
                developBranchNames.Any(name => name.Equals(branchName.Name, StringComparison.InvariantCultureIgnoreCase));

            return isDevelopBranch;
        }

        public static bool IsProductionBranch(this BranchName branchName)
        {
            var isProductionBranch = branchName.Name.Equals("master", StringComparison.InvariantCultureIgnoreCase) ||
                                     branchName.Name.IndexOf("release", StringComparison.InvariantCultureIgnoreCase) >=
                                     0;

            return isProductionBranch;
        }

        public static BranchName GetLogicalName(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                throw new ArgumentNullException("branchName");
            }

            var logicalName = branchName.Replace("refs/heads/", "");

            return new BranchName(logicalName);
        }

        public static bool BranchNameHasVersion(string branchName)
        {
            SemVersion version = BranchSemVerMajorMinorPatch(branchName);

            return version != null && (version.Major > 0 || version.Minor > 0 || version.Patch > 0);
        }

        public static SemVersion BranchSemVerMajorMinorPatch(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                return new SemVersion(0);
            }

            var splitCharactersVariable = Environment.GetEnvironmentVariable(WellKnownVariables.NameVersionCommanSeparatedSplitList);

            var splitCharacters = new List<string> {"/", "-", "_"};

            if (!string.IsNullOrWhiteSpace(splitCharactersVariable))
            {
                var splitts = splitCharactersVariable.Split(new[]{","}, StringSplitOptions.RemoveEmptyEntries).ToList();

                splitCharacters = splitts;
            }
            
            var version = branchName.Split(splitCharacters.ToArray(), StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

            SemVersion semver;
            if (!SemVersion.TryParse(version, out semver))
            {
                return new SemVersion(0);
            }
            
            return new SemVersion(semver.Major, semver.Minor, semver.Patch);
        }
    }
}