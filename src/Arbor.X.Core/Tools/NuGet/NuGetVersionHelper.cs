using System;
using Arbor.Build.Core.GenericExtensions;
using NuGet.Versioning;
using Serilog;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class NuGetVersionHelper
    {
        public static string GetVersion(
            string version,
            bool isReleaseBuild,
            string suffix,
            bool enableBuildNumber,
            string packageBuildMetadata,
            ILogger logger,
            NuGetVersioningSettings nugetVersioningSettings)
        {
            if (!Version.TryParse(version, out Version parsedVersion))
            {
                throw new ArgumentException($"The version '{version} is not a valid version format");
            }

            if (isReleaseBuild)
            {
                string parsed = parsedVersion.ToString(3);

                logger?.Information("Build is release build, using major.minor.patch as the version, {Parsed}", parsed);

                return parsed;
            }

            string buildVersion;

            int usePadding =
                nugetVersioningSettings.SemVerVersion == 1 && nugetVersioningSettings.MaxZeroPaddingLength > 0
                    ? nugetVersioningSettings.MaxZeroPaddingLength
                    : 0;

            string semVer2PreReleaseSeparator = nugetVersioningSettings.SemVerVersion == 2 ? "." : string.Empty;

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                if (enableBuildNumber)
                {
                    buildVersion =
                        $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{suffix}{semVer2PreReleaseSeparator}{parsedVersion.Revision.ToString().LeftPad(usePadding, '0')}";

                    logger?.Information(
                        "Package suffix is {Suffix}, using major.minor.patch-{suffix}build as the version, {BuildVersion}",
                        suffix,
                        null,
                        buildVersion);
                }
                else
                {
                    buildVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{suffix}";

                    logger?.Information(
                        "Package suffix is {Suffix}, using major.minor.patch-{suffix} as the version, {BuildVersion}",
                        suffix,
                        null,
                        buildVersion);
                }
            }
            else
            {
                if (enableBuildNumber)
                {
                    buildVersion =
                        $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{parsedVersion.Revision.ToString().LeftPad(usePadding, '0')}";

                    logger?.Information("Using major.minor.patch-build as the version, {BuildVersion}", buildVersion);
                }
                else
                {
                    buildVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}";
                    logger?.Information("Using major.minor.patch as the version, {BuildVersion}", buildVersion);
                }
            }

            string final;

            if (!string.IsNullOrWhiteSpace(packageBuildMetadata))
            {
                final = $"{buildVersion}+{packageBuildMetadata.TrimStart('+')}";
            }
            else
            {
                final = buildVersion;
            }

            if (!SemanticVersion.TryParse(final, out SemanticVersion semanticVersion))
            {
                throw new InvalidOperationException($"The NuGet version '{final}' is not a valid Semver 2.0 version");
            }

            return final;
        }
    }
}
