using System;

using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.MSBuild
{
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