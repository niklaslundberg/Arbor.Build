using System;
using System.Runtime.InteropServices;

namespace Arbor.Build.Core.Tools.Platform;

public static class PlatformHelper
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public static string ExecutableExtension => IsWindows ? ".exe" : string.Empty;

    public static string GetExecutableName(string baseName) => $"{baseName}{ExecutableExtension}";
}
