using System;

namespace Arbor.X.Core.Tools.NuGet
{
    public static class NuGetVersionHelper
    {
        public static string GetVersion(string version, bool isReleaseBuild, string suffix, bool enableBuildNumber)
        {
            Version parsedVersion;
            if (!Version.TryParse(version, out parsedVersion))
            {
                throw new ArgumentException(string.Format("The version '{0} is not a valid version format", version));
            }
            
            if (isReleaseBuild)
            {
                return parsedVersion.ToString(fieldCount: 3);
            }
            string buildVersion;

            if (!string.IsNullOrWhiteSpace(suffix))
            {
                if (enableBuildNumber)
                {
                    buildVersion = string.Format("{0}.{1}.{2}-{3}{4}", parsedVersion.Major, parsedVersion.Minor,
                        parsedVersion.Build, suffix, parsedVersion.Revision);
                }
                else
                {
                    buildVersion = string.Format("{0}.{1}.{2}-{3}", parsedVersion.Major, parsedVersion.Minor,
                        parsedVersion.Build, suffix);
                }
            }
            else
            {
                if (enableBuildNumber)
                {
                    buildVersion = string.Format("{0}.{1}.{2}-{3}", parsedVersion.Major, parsedVersion.Minor,
                        parsedVersion.Build, parsedVersion.Revision);
                }
                else
                {
                    buildVersion = string.Format("{0}.{1}.{2}", parsedVersion.Major, parsedVersion.Minor,
                        parsedVersion.Build);
                }
            }
            return buildVersion;
        }
    }
} ;