using System;

namespace Arbor.Build.Core.Tools.NuGet;

public class NuGetVersioningSettings
{
    private static readonly Lazy<NuGetVersioningSettings> Lazy = new(() =>
        new NuGetVersioningSettings
        {
            MaxZeroPaddingLength = 0,
            SemVerVersion = 2
        });

    public int MaxZeroPaddingLength { get; set; }

    public int SemVerVersion { get; set; }

    public static NuGetVersioningSettings Default => Lazy.Value;
}