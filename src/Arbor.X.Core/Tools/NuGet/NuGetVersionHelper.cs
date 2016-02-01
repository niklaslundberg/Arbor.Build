using System;
using System.Collections.Generic;

using Arbor.X.Core.Logging;

using NUnit.Framework;

namespace Arbor.X.Core.Tools.NuGet
{
    public static class NuGetVersionHelper
    {
        public static string GetVersion(string version, bool isReleaseBuild, string suffix, bool enableBuildNumber, string packageBuildMetadata, ILogger logger)
        {
            Version parsedVersion;
            if (!Version.TryParse(version, out parsedVersion))
            {
                throw new ArgumentException($"The version '{version} is not a valid version format");
            }

            if (isReleaseBuild)
            {
                string parsed = parsedVersion.ToString(fieldCount: 3);

                logger.Write($"Build is release build, using major.minor.patch as the version, {parsed}");

                return parsed;
            }
            string buildVersion;

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                if (enableBuildNumber)
                {
                    buildVersion =
                        $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{suffix}.{parsedVersion.Revision}";

                    logger.Write($"Package suffix is {suffix}, using major.minor.patch-{{suffix}}build as the version, {buildVersion}");

                }
                else
                {
                    buildVersion = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{suffix}";

                    logger.Write($"Package suffix is {suffix}, using major.minor.patch-{{suffix}} as the version, {buildVersion}");
                }
            }
            else
            {
                if (enableBuildNumber)
                {
                    buildVersion =
                        $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build}-{parsedVersion.Revision}";

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
               final= $"{buildVersion}+{packageBuildMetadata.TrimStart('+')}";
            }
            else
            {
                final = buildVersion;
            }

            return final;
        }
    }
} ;
