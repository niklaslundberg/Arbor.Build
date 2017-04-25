using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.X.Core.BuildVariables;
using NuGet.Versioning;

namespace Arbor.X.Core.Tools.Git
{
    public static class BranchHelper
    {
        public static bool IsFeatureBranch(this BranchName branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var nonFeatureBranchNames = new[]
            {
                "dev",
                "develop",
                "master",
                "release",
                "hotfix"
            };

            bool isAStandardBranch =
                nonFeatureBranchNames.Any(
                    name => branchName.LogicalName.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));

            return !isAStandardBranch;
        }

        public static bool IsDevelopBranch(this BranchName branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var developBranchNames = new[]
            {
                "develop",
                "dev"
            };

            bool isDevelopBranch =
                developBranchNames.Any(name => branchName.LogicalName.StartsWith(branchName.Name,
                    StringComparison.InvariantCultureIgnoreCase));

            return isDevelopBranch;
        }

        public static bool IsProductionBranch(this BranchName branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            var productionBranches = new List<string>(10)
            {
                "master",
                "release",
                "hotfix"
            };

            bool isProductionBranch =
                productionBranches.Any(productionBranch => branchName.LogicalName.StartsWith(productionBranch,
                    StringComparison.InvariantCultureIgnoreCase));

            return isProductionBranch;
        }

        public static BranchName GetLogicalName(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            string logicalName = branchName.Replace("refs/heads/", string.Empty);

            return new BranchName(logicalName);
        }

        public static bool BranchNameHasVersion(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            SemanticVersion version = BranchSemVerMajorMinorPatch(branchName);

            return version != null && (version.Major > 0 || version.Minor > 0 || version.Patch > 0);
        }

        public static SemanticVersion BranchSemVerMajorMinorPatch(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            if (string.IsNullOrWhiteSpace(branchName))
            {
                return new SemanticVersion(0, 0, 0);
            }

            string splitCharactersVariable =
                Environment.GetEnvironmentVariable(WellKnownVariables.NameVersionCommanSeparatedSplitList);

            var splitCharacters = new List<string>
            {
                "/",
                "-",
                "_"
            };

            if (!string.IsNullOrWhiteSpace(splitCharactersVariable))
            {
                List<string> splitts =
                    splitCharactersVariable.Split(
                            new[]
                            {
                                ","
                            },
                            StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                splitCharacters = splitts;
            }

            string version =
                branchName.Split(splitCharacters.ToArray(), StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

            SemanticVersion semver;
            if (!SemanticVersion.TryParse(version, out semver))
            {
                return new SemanticVersion(0, 0, 0);
            }

            return new SemanticVersion(semver.Major, semver.Minor, semver.Patch);
        }
    }
}
