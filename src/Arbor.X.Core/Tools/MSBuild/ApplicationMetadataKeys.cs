namespace Arbor.X.Core.Tools.MSBuild
{
    internal class ApplicationMetadataKeys
    {
        public static string GitHash = "urn:versioning:vcs:git:hash";

        public static string GitBranch = "urn:versioning:vcs:git:branch";

        public static string DotNetCpuPlatform = "urn:dotnet:runtime:cpu-platform";

        public static string DotNetConfiguration = "urn:dotnet:compilation:configuration";
    }
}
