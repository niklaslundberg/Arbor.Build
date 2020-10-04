using System;
using System.IO;
using Arbor.Build.Core.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NuGet.Versioning;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class NuGetPackageConfiguration
    {
        public NuGetPackageConfiguration(
            [NotNull] string configuration,
            [NotNull] SemanticVersion version,
            [NotNull] DirectoryEntry packagesDirectory,
            [NotNull] UPath nugetExePath,
            string branchName,
            string? suffix = null,
            bool branchNameEnabled = false,
            string? packageIdOverride = null,
            string? nuGetPackageVersionOverride = null,
            bool allowManifestReWrite = false,
            bool nuGetSymbolPackagesEnabled = false,
            bool keepBinaryAndSourcePackagesTogetherEnabled = false,
            bool isReleaseBuild = false,
            bool buildNumberEnabled = true,
            DirectoryEntry? tempPath = null,
            string? packageBuildMetadata = null,
            string nuGetSymbolPackagesFormat = NuGetPackager.SnupkgPackageFormat,
            string? packageNameSuffix = null,
            string? gitHash = null)
        {
            if (string.IsNullOrWhiteSpace(configuration))
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (packagesDirectory is null)
            {
                throw new ArgumentNullException(nameof(packagesDirectory));
            }

            if (nugetExePath == UPath.Empty)
            {
                throw new ArgumentNullException(nameof(nugetExePath));
            }

            if (!packagesDirectory.FileSystem.FileExists(nugetExePath))
            {
                throw new FileNotFoundException($"Could not find NuGet exe path, '{nugetExePath}'");
            }

            if (!packagesDirectory.Exists)
            {
                throw new DirectoryNotFoundException($"Could not find package directory, '{packagesDirectory}'");
            }

            Configuration = configuration;
            KeepBinaryAndSourcePackagesTogetherEnabled = keepBinaryAndSourcePackagesTogetherEnabled;
            IsReleaseBuild = isReleaseBuild;
            BranchName = branchName;
            BuildNumberEnabled = buildNumberEnabled;
            PackageBuildMetadata = packageBuildMetadata;
            TempPath = tempPath ?? new DirectoryEntry(packagesDirectory.FileSystem, UPath.Combine(Path.GetTempPath().AsFullPath(), $"{DefaultPaths.TempPathPrefix}_Nuget")).EnsureExists();
            BranchNameEnabled = branchNameEnabled;
            PackageIdOverride = packageIdOverride;
            NuGetPackageVersionOverride = nuGetPackageVersionOverride;
            AllowManifestReWrite = allowManifestReWrite;
            NuGetSymbolPackagesEnabled = nuGetSymbolPackagesEnabled;
            Version = version ?? throw new ArgumentNullException(nameof(version));
            PackagesDirectory = packagesDirectory;
            NuGetExePath = nugetExePath;
            NuGetSymbolPackagesFormat = nuGetSymbolPackagesFormat;
            Suffix = suffix;
            PackageNameSuffix = packageNameSuffix;
            GitHash = gitHash;
        }

        public string? PackageNameSuffix { get; }
        public string? GitHash { get; }

        public bool KeepBinaryAndSourcePackagesTogetherEnabled { get; }

        public bool BranchNameEnabled { get; }

        public string? PackageIdOverride { get; }

        public string? NuGetPackageVersionOverride { get; }

        public bool AllowManifestReWrite { get; }

        public bool NuGetSymbolPackagesEnabled { get; set; }

        public string? Configuration { get; }

        public bool IsReleaseBuild { get; }

        public string BranchName { get; }

        public SemanticVersion Version { get; }

        public string? Suffix { get; }

        public DirectoryEntry TempPath { get; }

        public UPath NuGetExePath { get; }

        public DirectoryEntry PackagesDirectory { get; }

        public bool BuildNumberEnabled { get; }

        public string? PackageBuildMetadata { get; }

        public string NuGetSymbolPackagesFormat { get; }

        public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
