namespace Arbor.X.Core.Tools.MSBuild
{
    internal class ApplicationMetadataKeys
    {
        public static readonly string GitHash = "urn:versioning:vcs:git:hash";

        public static readonly string GitBranch = "urn:versioning:vcs:git:branch";

        public static readonly string DotNetCpuPlatform = "urn:dotnet:runtime:cpu-platform";

        public static readonly string DotNetConfiguration = "urn:dotnet:compilation:configuration";
    }
}
