using System;

namespace Arbor.X.Core.Tools.NuGet
{
    public static class NuGetVersionHelper
    {
        public static string GetVersion(string version, bool isReleaseBuild)
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

            string buildVersion = string.Format("{0}.{1}.{2}-build{3}", parsedVersion.Major, parsedVersion.Minor, parsedVersion.Build, parsedVersion.Revision);

            return buildVersion;
        }
    }
} ;