using System;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class NuGetVersioningSettings
    {
        public int MaxZeroPaddingLength { get; set; }

        public int SemVerVersion { get; set; }

        private static readonly Lazy<NuGetVersioningSettings> _Lazy = new Lazy<NuGetVersioningSettings>(() => new NuGetVersioningSettings()
        {
            MaxZeroPaddingLength = 0,
            SemVerVersion = 2
        });

        public static NuGetVersioningSettings Default => _Lazy.Value;
    }
}
