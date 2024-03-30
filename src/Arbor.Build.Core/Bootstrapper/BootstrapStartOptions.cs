using System;
using System.Collections.Immutable;
using System.Linq;
using Zio;

namespace Arbor.Build.Core.Bootstrapper;

public class BootstrapStartOptions(
    string[] args,
    DirectoryEntry? baseDir = null,
    bool? preReleaseEnabled = null,
    string? branchName = null,
    bool downloadOnly = false,
    string? arborBuildExePath = default,
    string? nuGetConfig = default)
{
    public const string DownloadOnlyCliParameter = "--download-only";
    public const string ArborBuildExeCliParameter = "-arborBuildExe=";

    public bool? PreReleaseEnabled { get; } = preReleaseEnabled;

    public ImmutableArray<string> Args { get; } = args.ToImmutableArray();

    public DirectoryEntry? BaseDir { get; } = baseDir;

    public string? BranchName { get; } = branchName;

    public bool DownloadOnly { get; } = downloadOnly;

    public string? ArborBuildExePath { get; } = arborBuildExePath;
    public string? NuGetConfig { get; } = nuGetConfig;

    public static BootstrapStartOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        bool downloadOnly = args.Any(arg => arg.Equals(DownloadOnlyCliParameter, StringComparison.OrdinalIgnoreCase));

        string? arborBuildExePath = args.FirstOrDefault(arg => arg.StartsWith(ArborBuildExeCliParameter, StringComparison.OrdinalIgnoreCase))?.Split("=").Skip(1).FirstOrDefault();

        return new BootstrapStartOptions(args, downloadOnly: downloadOnly, arborBuildExePath: arborBuildExePath);
    }
}