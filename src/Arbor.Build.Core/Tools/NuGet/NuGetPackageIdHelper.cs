using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.Tools.Git;

namespace Arbor.Build.Core.Tools.NuGet;

public static class NuGetPackageIdHelper
{
    public static string CreateNugetPackageId(string basePackageId,
        NuGetPackageConfiguration packageConfiguration)
    {
        if (string.IsNullOrWhiteSpace(basePackageId))
        {
            throw new ArgumentNullException(nameof(basePackageId));
        }

        if (!string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
        {
            return packageConfiguration.PackageIdOverride;
        }

        if (packageConfiguration.BranchNameEnabled && !string.IsNullOrWhiteSpace(packageConfiguration.BranchName) && new BranchName(packageConfiguration.BranchName).IsFeatureBranch())
        {
            return CreateNugetPackageIdWithBranchName(basePackageId,
                packageConfiguration) + packageConfiguration.PackageNameSuffix;
        }

        return basePackageId + packageConfiguration.PackageNameSuffix;
    }

    private static string CreateNugetPackageIdWithBranchName(string basePackageId,
        NuGetPackageConfiguration packageConfiguration)
    {
        var branch = new BranchName(packageConfiguration.BranchName);

        if (packageConfiguration.IsReleaseBuild || !branch.IsFeatureBranch())
        {
            return basePackageId + packageConfiguration.PackageNameSuffix;
        }

        string normalizedBranchName = branch.Normalize();

        string nugetPackageId = $"{basePackageId}-{normalizedBranchName}";

        var invalidCharacters = new List<string>
        {
            "<",
            "@",
            ">",
            "|",
            "?",
            ":"
        };

        return invalidCharacters.Aggregate(
            nugetPackageId,
            (current, invalidCharacter) =>
                current.Replace(invalidCharacter, string.Empty, StringComparison.Ordinal));
    }
}