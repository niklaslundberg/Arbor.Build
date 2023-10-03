using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.BuildVariables;
using NuGet.Versioning;

namespace Arbor.Build.Core.Tools.Git;

public static class BranchHelper
{
    public static bool IsFeatureBranch(this BranchName branchName)
    {
        if (branchName == null)
        {
            throw new ArgumentNullException(nameof(branchName));
        }

        string[] nonFeatureBranchNames =
        {
            "dev", BranchName.Develop.LogicalName, BranchName.Master.LogicalName, BranchName.Main.LogicalName,
            "release", "hotfix"
        };

        bool isAStandardBranch = nonFeatureBranchNames.Any(name =>
            branchName.LogicalName.StartsWith(name, StringComparison.OrdinalIgnoreCase));

        return !isAStandardBranch;
    }

    public static bool IsDevelopBranch(this BranchName branchName)
    {
        if (branchName == null)
        {
            throw new ArgumentNullException(nameof(branchName));
        }

        string[] developBranchNames = { BranchName.Develop.LogicalName, "dev" };

        return developBranchNames.Any(name =>
            branchName.LogicalName.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsProductionBranch(this BranchName branchName)
    {
        if (branchName == null)
        {
            throw new ArgumentNullException(nameof(branchName));
        }

        var productionBranches = new List<string>(10)
        {
            BranchName.Master.LogicalName, BranchName.Main.LogicalName, "release", "hotfix"
        };

        return productionBranches.Any(productionBranch =>
            branchName.LogicalName.StartsWith(productionBranch, StringComparison.OrdinalIgnoreCase));
    }

    public static BranchName GetLogicalName(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new ArgumentNullException(nameof(branchName));
        }

        string logicalName = branchName.Replace("refs/heads/", string.Empty, StringComparison.Ordinal);

        return new BranchName(logicalName);
    }

    public static bool BranchNameHasVersion(string branchName, IEnvironmentVariables environmentVariables)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new ArgumentNullException(nameof(branchName));
        }

        var version = BranchSemVerMajorMinorPatch(branchName, environmentVariables);

        if (version is null)
        {
            return false;
        }

        return version.Major > 0 || version.Minor > 0 || version.Patch > 0;
    }

    public static SemanticVersion? BranchSemVerMajorMinorPatch(string? branchName,
        IEnvironmentVariables environmentVariables)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new ArgumentNullException(nameof(branchName));
        }

        if (branchName.Contains("dependabot/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string? splitCharactersVariable =
            environmentVariables.GetEnvironmentVariable(WellKnownVariables.NameVersionCommonSeparatedSplitList);

        var splitCharacters = new List<string> { "/", "-", "_" };

        if (!string.IsNullOrWhiteSpace(splitCharactersVariable))
        {
            splitCharacters = splitCharactersVariable.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        string? version = branchName.Split(splitCharacters.ToArray(), StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        if (!SemanticVersion.TryParse(version!, out SemanticVersion? semver))
        {
            return new SemanticVersion(0, 0, 0);
        }

        return new SemanticVersion(semver.Major, semver.Minor, semver.Patch);
    }
}