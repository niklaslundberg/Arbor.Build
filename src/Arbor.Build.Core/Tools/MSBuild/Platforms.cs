using System;

namespace Arbor.Build.Core.Tools.MSBuild;

public static class Platforms
{
    public static string Normalize(string platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new ArgumentNullException(nameof(platform));
        }

        return platform.Replace(" ", string.Empty, StringComparison.InvariantCultureIgnoreCase);
    }
}