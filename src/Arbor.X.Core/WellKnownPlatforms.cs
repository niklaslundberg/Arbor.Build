using System;

using JetBrains.Annotations;

namespace Arbor.X.Core
{
    public static class WellKnownPlatforms
    {
        public const string AnyCPU = "Any CPU";
    }

    public static class Platforms
    {
        public static string Normalize([NotNull] string platform)
        {
            if (string.IsNullOrWhiteSpace(platform))
            {
                throw new ArgumentNullException(nameof(platform));
            }
            return platform.Replace(" ", "");
        }
    }
}