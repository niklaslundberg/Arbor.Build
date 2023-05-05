namespace Arbor.Build.Core.Tools.MSBuild;

internal static class ApplicationMetadataKeys
{
    public const string GitHash = "urn:versioning:vcs:git:hash";

    public const string GitBranch = "urn:versioning:vcs:git:branch";

    public const string DotNetCpuPlatform = "urn:dotnet:runtime:cpu-platform";

    public const string DotNetConfiguration = "urn:dotnet:compilation:configuration";
}