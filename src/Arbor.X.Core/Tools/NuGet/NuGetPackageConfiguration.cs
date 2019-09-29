using System;
using System.IO;
using Arbor.Build.Core.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NuGet.Versioning;

namespace Arbor.Build.Core.Tools.NuGet
{
    public class NuGetPackageConfiguration
    {
        public NuGetPackageConfiguration(
            [NotNull] string configuration,
            [NotNull] SemanticVersion version,
            [NotNull] string packagesDirectory,
            [NotNull] string nugetExePath,
            string? suffix = null,
            bool branchNameEnabled = false,
            string? packageIdOverride = null,
            string? nuGetPackageVersionOverride = null,
            bool allowManifestReWrite = false,
            bool nuGetSymbolPackagesEnabled = false,
            bool keepBinaryAndSourcePackagesTogetherEnabled = false,
            bool isReleaseBuild = false,
            string? branchName = null,
            bool buildNumberEnabled = true,
            string tempPath = null,
            string packageBuildMetadata = null,
            string nuGetSymbolPackagesFormat = NuGetPackager.SnupkgPackageFormat)
        {
            if (string.IsNullOrWhiteSpace(configuration))
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(packagesDirectory))
            {
                throw new ArgumentNullException(nameof(packagesDirectory));
            }

            if (string.IsNullOrWhiteSpace(nugetExePath))
            {
                throw new ArgumentNullException(nameof(nugetExePath));
            }

            if (!File.Exists(nugetExePath))
            {
                throw new FileNotFoundException($"Could not find NuGet exe path, '{nugetExePath}'");
            }

            if (!Directory.Exists(packagesDirectory))
            {
                throw new DirectoryNotFoundException($"Could not find package directory, '{packagesDirectory}'");
            }

            Configuration = configuration;
            KeepBinaryAndSourcePackagesTogetherEnabled = keepBinaryAndSourcePackagesTogetherEnabled;
            IsReleaseBuild = isReleaseBuild;
            BranchName = branchName;
            BuildNumberEnabled = buildNumberEnabled;
            PackageBuildMetadata = packageBuildMetadata;
            TempPath = tempPath ?? Path.Combine(Path.GetTempPath(), $"{DefaultPaths.TempPathPrefix}_Nuget");
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
        }

        public bool KeepBinaryAndSourcePackagesTogetherEnabled { get; }

        public bool BranchNameEnabled { get; }

        public string? PackageIdOverride { get; }

        public string? NuGetPackageVersionOverride { get; }

        public bool AllowManifestReWrite { get; }

        public bool NuGetSymbolPackagesEnabled { get; set; }

        public string Configuration { get; }

        public bool IsReleaseBuild { get; }

        public string BranchName { get; }

        public SemanticVersion Version { get; }

        public string? Suffix { get; }

        public string TempPath { get; }

        public string NuGetExePath { get; }

        public string PackagesDirectory { get; }

        public bool BuildNumberEnabled { get; }

        public string? PackageBuildMetadata { get; }

        public string NuGetSymbolPackagesFormat { get; }

        public override string ToString() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
