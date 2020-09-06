using System;
using System.Globalization;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.MSBuild;
using NuGet.Versioning;
using Serilog;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class NuGetVersionHelper
    {
        public static string GetVersion(
            string version,
            bool isReleaseBuild,
            string? suffix,
            bool enableBuildNumber,
            string? packageBuildMetadata,
            ILogger? logger,
            NuGetVersioningSettings? nugetVersioningSettings = null,
            GitBranchModel? gitModel = null,
            BranchName? branchName = null)
        {
            string GetStableSemanticVersion(Version result)
            {
                string parsed = result.ToString(3);

                return parsed;
            }

            if (!Version.TryParse(version, out Version? parsedVersion))
            {
                throw new ArgumentException($"The version '{version} is not a valid version format");
            }

            if (isReleaseBuild && GitBranchModel.GitFlowBuildOnMain != gitModel)
            {
                string parsed = GetStableSemanticVersion(parsedVersion);

                logger?.Information("Build is release build, using major.minor.patch as the version, {Parsed}", parsed);

                return parsed;
            }

            string buildVersion;

            var settings = nugetVersioningSettings ?? NuGetVersioningSettings.Default;

            int usePadding =
                settings.SemVerVersion == 1 && settings.MaxZeroPaddingLength > 0
                    ? settings.MaxZeroPaddingLength
                    : 0;

            string semVer2PreReleaseSeparator = settings.SemVerVersion >= 2
                ? "."
                : string.Empty;

            if (GitBranchModel.GitFlowBuildOnMain == gitModel && isReleaseBuild)
            {
                if (branchName is {IsMainBranch: true})
                {
                    return GetStableSemanticVersion(parsedVersion);
                }

                suffix ??= "rc";
            }
            else
            {
                suffix ??= "build";
            }

            if (suffix.Length > 0)
            {
                if (enableBuildNumber)
                {
                    buildVersion =
                        $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{suffix}{semVer2PreReleaseSeparator}{parsedVersion.Revision.ToString(CultureInfo.InvariantCulture).LeftPad(usePadding, '0')}";

                    logger?.Information(
                        "Package suffix is {Suffix}, using major.minor.patch-{UsedSuffix}build as the version, {BuildVersion}",
                        suffix,
                        suffix,
                        buildVersion);
                }
                else
                {
                    buildVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{suffix}";

                    logger?.Information(
                        "Package suffix is {Suffix}, using major.minor.patch-{UsedSuffix} as the version, {BuildVersion}",
                        suffix,
                        suffix,
                        buildVersion);
                }
            }
            else
            {
                if (enableBuildNumber)
                {
                    buildVersion =
                        $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{parsedVersion.Revision.ToString(CultureInfo.InvariantCulture).LeftPad(usePadding, '0')}";

                    logger?.Information("Using major.minor.patch-build as the version, {BuildVersion}", buildVersion);
                }
                else
                {
                    buildVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}";
                    logger?.Information("Using major.minor.patch as the version, {BuildVersion}", buildVersion);
                }
            }

            string final = !string.IsNullOrWhiteSpace(packageBuildMetadata)
                ? $"{buildVersion}+{packageBuildMetadata.TrimStart('+')}"
                : buildVersion;

            if (!SemanticVersion.TryParse(final, out SemanticVersion _))
            {
                throw new InvalidOperationException($"The NuGet version '{final}' is not a valid Semver 2.0 version");
            }

            return final;
        }

        public static string GetPackageVersion(VersionOptions versionOptions)
        {
            string version = GetVersion(
                versionOptions.Version,
                versionOptions.IsReleaseBuild,
                versionOptions.BuildSuffix,
                versionOptions.BuildNumberEnabled,
                versionOptions.Metadata,
                versionOptions.Logger,
                versionOptions.NuGetVersioningSettings,
                versionOptions.GitModel,
                versionOptions.BranchName);

            string packageVersion = SemanticVersion.Parse(
                version).ToNormalizedString();

            return packageVersion;
        }
    }
}
