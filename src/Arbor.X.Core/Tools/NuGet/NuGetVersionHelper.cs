using System;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;
using NuGet.Versioning;

namespace Arbor.X.Core.Tools.NuGet
{
    public static class NuGetVersionHelper
    {
        public static string GetVersion(string version, bool isReleaseBuild, string suffix, bool enableBuildNumber,
            string packageBuildMetadata, ILogger logger, NuGetVersioningSettings nugetVersioningSettings)
        {
            Version parsedVersion;
            if (!Version.TryParse(version, out parsedVersion))
            {
                throw new ArgumentException($"The version '{version} is not a valid version format");
            }

            if (isReleaseBuild)
            {
                string parsed = parsedVersion.ToString(3);

                logger.Write($"Build is release build, using major.minor.patch as the version, {parsed}");

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

                    logger.Write(
                        $"Package suffix is {suffix}, using major.minor.patch-{{suffix}}build as the version, {buildVersion}");
                }
                else
                {
                    buildVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{suffix}";

                    logger.Write(
                        $"Package suffix is {suffix}, using major.minor.patch-{{suffix}} as the version, {buildVersion}");
                }
            }
            else
            {
                if (enableBuildNumber)
                {
                    buildVersion =
                        $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{parsedVersion.Revision.ToString().LeftPad(usePadding, '0')}";

                    logger.Write($"Using major.minor.patch-build as the version, {buildVersion}");
                }
                else
                {
                    buildVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}";
                    logger.Write($"Using major.minor.patch as the version, {buildVersion}");
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

            SemanticVersion semanticVersion;

            if (!SemanticVersion.TryParse(final, out semanticVersion))
            {
                throw new InvalidOperationException($"The NuGet version '{final}' is not a valid Semver 2.0 version");
            }

            return final;
        }
    }

    public class NuGetVersioningSettings
    {
        public int MaxZeroPaddingLength { get; set; }

        public int SemVerVersion { get; set; }
    }
}
