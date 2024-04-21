using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.NuGet;
using Serilog;

namespace Arbor.Build.Core.Tools.MSBuild;

public class VersionOptions(string version)
{
    public GitBranchModel? GitModel { get; set; }

    public string Version { get; } = version;

    public bool IsReleaseBuild { get; set; }

    public string? BuildSuffix { get; set; }

    public bool BuildNumberEnabled { get; set; } = true;

    public string? Metadata { get; set; }

    public ILogger? Logger { get; set; }

    public NuGetVersioningSettings NuGetVersioningSettings { get; set; } = NuGetVersioningSettings.Default;

    public BranchName? BranchName { get; set; }
}