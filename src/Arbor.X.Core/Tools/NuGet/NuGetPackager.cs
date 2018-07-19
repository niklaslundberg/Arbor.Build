using System; using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;

using Arbor.X.Core.Tools.Git;
using Serilog.Events;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NuGetPackager
    {
        private readonly ILogger _logger;

        public NuGetPackager(ILogger logger)
        {
            _logger = logger;
        }

        public static NuGetPackageConfiguration GetNuGetPackageConfiguration(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string packagesDirectory,
            string vcsRootDir)
        {
            IVariable version = buildVariables.Require(WellKnownVariables.Version).ThrowIfEmptyValue();
            IVariable releaseBuild = buildVariables.Require(WellKnownVariables.ReleaseBuild).ThrowIfEmptyValue();
            IVariable branchName = buildVariables.Require(WellKnownVariables.BranchLogicalName).ThrowIfEmptyValue();
            string configuration = buildVariables.Require(WellKnownVariables.Configuration).ThrowIfEmptyValue().Value;
            IVariable tempDirectory = buildVariables.Require(WellKnownVariables.TempDirectory).ThrowIfEmptyValue();
            string nuGetExePath = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath)
                .ThrowIfEmptyValue()
                .Value;

            string suffix =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageArtifactsSuffix, "build");
            bool buildNumberEnabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.BuildNumberInNuGetPackageArtifactsEnabled,
                true);

            bool keepBinaryAndSourcePackagesTogetherEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled,
                    true);
            bool branchNameEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageIdBranchNameEnabled, false);
            string packageIdOverride =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageIdOverride, null);
            bool nuGetSymbolPackagesEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetSymbolPackagesEnabled, true);
            string nuGetPackageVersionOverride =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageVersionOverride, null);
            bool allowManifestReWrite =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetAllowManifestReWrite, false);

            bool buildPackagesOnAnyBranch =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled, false);

            if (!buildPackagesOnAnyBranch)
            {
                if (branchName.Value.Equals("master", StringComparison.InvariantCultureIgnoreCase))
                {
                    logger.Warning("NuGet package creation is not supported on 'master' branch. To force NuGet package creation, set environment variable '{NuGetCreatePackagesOnAnyBranchEnabled}' to value 'true'", WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled);

                    // return ExitCode.Success;
                }
            }
            else
            {
                logger.Verbose("Flag '{NuGetCreatePackagesOnAnyBranchEnabled}' is set to true, creating NuGet packages", WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled);
            }

            Maybe<BranchName> branchNameMayBe = BranchName.TryParse(branchName.Value);

            if (!branchNameMayBe.HasValue)
            {
                throw new InvalidOperationException("The branchname could not be found");
            }

            bool isReleaseBuild = IsReleaseBuild(releaseBuild.Value, branchNameMayBe.Value);

            logger.Verbose("Based on branch {Value} and release build flags {Value1}, the build is considered {V}", branchName.Value, releaseBuild.Value, (isReleaseBuild ? "release" : "not release"));

            if (configuration.Equals("debug", StringComparison.InvariantCultureIgnoreCase) && isReleaseBuild)
            {
                logger.Information(
                    "The current configuration is 'debug' but the build indicates that this is a release build, using 'release' configuration instead");
                configuration = "release";
            }

            if (!Directory.Exists(packagesDirectory))
            {
                Directory.CreateDirectory(packagesDirectory);
            }

            if (!File.Exists(nuGetExePath))
            {
                logger.Error("The NuGet.exe path {NuGetExePath} was not found or NuGet could not be downloaded", nuGetExePath);

                // return ExitCode.Failure;
            }

            logger.Verbose("Scanning directory '{VcsRootDir}' for .nuspec files", vcsRootDir);

            var packageConfiguration = new NuGetPackageConfiguration(
                configuration,
                version.Value,
                packagesDirectory,
                nuGetExePath,
                suffix,
                branchNameEnabled,
                packageIdOverride,
                nuGetPackageVersionOverride,
                allowManifestReWrite,
                nuGetSymbolPackagesEnabled,
                keepBinaryAndSourcePackagesTogetherEnabled,
                isReleaseBuild,
                branchName.Value,
                buildNumberEnabled,
                tempDirectory.Value);
            return packageConfiguration;
        }

        public async Task<ExitCode> CreatePackageAsync(
            string packageSpecificationPath,
            NuGetPackageConfiguration packageConfiguration,
            bool ignoreWarnings = false,
            CancellationToken cancellationToken = default)
        {
            _logger.Debug("Using NuGet package configuration {PackageConfiguration}", packageConfiguration);

            NuSpec nuSpec = NuSpec.Parse(packageSpecificationPath);

            string properties = GetProperties(packageConfiguration.Configuration);

            if (!string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
            {
                _logger.Information("Using NuGet package id override '{PackageIdOverride}'", packageConfiguration.PackageIdOverride);
            }

            string packageId = !string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride)
                ? packageConfiguration.PackageIdOverride
                : NuGetPackageIdHelper.CreateNugetPackageId(
                    nuSpec.PackageId,
                    packageConfiguration.IsReleaseBuild,
                    packageConfiguration.BranchName,
                    packageConfiguration.BranchNameEnabled);

            if (string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
            {
                _logger.Information("Using NuGet package ID {PackageId}", packageId);
            }
            else
            {
                _logger.Information("Using NuGet package version override '{PackageIdOverride}'", packageConfiguration.PackageIdOverride);
            }

            var nuGetVersioningSettings = new NuGetVersioningSettings { MaxZeroPaddingLength = 5, SemVerVersion = 1 };
            string nuGetPackageVersion = !string.IsNullOrWhiteSpace(packageConfiguration.NuGetPackageVersionOverride)
                ? packageConfiguration.NuGetPackageVersionOverride
                : NuGetVersionHelper.GetVersion(
                    packageConfiguration.Version,
                    packageConfiguration.IsReleaseBuild,
                    packageConfiguration.Suffix,
                    packageConfiguration.BuildNumberEnabled,
                    packageConfiguration.PackageBuildMetadata,
                    _logger,
                    nuGetVersioningSettings);

            _logger.Information(
                string.IsNullOrWhiteSpace(packageConfiguration.NuGetPackageVersionOverride)
                    ? $"Using NuGet package version {nuGetPackageVersion}"
                    : $"Using NuGet package version override '{packageConfiguration.NuGetPackageVersionOverride}'");

            var nuSpecInfo = new FileInfo(packageSpecificationPath);

            // ReSharper disable AssignNullToNotNullAttribute
            string nuSpecFileCopyPath = Path.Combine(nuSpecInfo.DirectoryName, $"{Guid.NewGuid()}-{nuSpecInfo.Name}");

            // ReSharper restore AssignNullToNotNullAttribute

            var nuSpecCopy = new NuSpec(packageId, nuGetPackageVersion, nuSpecInfo.FullName);

            string nuSpecTempDirectory = Path.Combine(packageConfiguration.TempPath, "nuspecs");

            if (!Directory.Exists(nuSpecTempDirectory))
            {
                Directory.CreateDirectory(nuSpecTempDirectory);
            }

            _logger.Verbose("Saving new nuspec '{NuSpecFileCopyPath}'", nuSpecFileCopyPath);
            nuSpecCopy.Save(nuSpecFileCopyPath);

            var removedTags = new List<string>();

            if (packageConfiguration.AllowManifestReWrite)
            {
                _logger.Verbose("Rewriting manifest in NuSpec '{NuSpecFileCopyPath}'", nuSpecFileCopyPath);

                var manitestReWriter = new ManitestReWriter();
                ManifestReWriteResult manifestReWriteResult = manitestReWriter.Rewrite(nuSpecFileCopyPath);

                removedTags.AddRange(manifestReWriteResult.RemoveTags);
            }
            else
            {
                _logger.Verbose("Rewriting manifest disabled");
            }

            _logger.Verbose("Created nuspec content: {NewLine}{V}", Environment.NewLine, File.ReadAllText(nuSpecFileCopyPath));

            ExitCode result = await ExecuteNuGetPackAsync(
                packageConfiguration.NuGetExePath,
                packageConfiguration.PackagesDirectory,
                _logger,
                nuSpecFileCopyPath,
                properties,
                nuSpecCopy,
                removedTags,
                cancellationToken: cancellationToken,
                ignoreWarnings: ignoreWarnings).ConfigureAwait(false);

            return result;
        }

        private static string GetProperties(string configuration)
        {
            var propertyValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(
                    "configuration",
                    configuration)
            };

            IEnumerable<string> formattedValues = propertyValues.Select(item => $"{item.Key}={item.Value}");
            string properties = string.Join(";", formattedValues);
            return properties;
        }

        private static bool IsReleaseBuild(string releaseBuild, BranchName branchName)
        {
            bool isReleaseBuild = releaseBuild.TryParseBool(false);

            return isReleaseBuild || branchName.IsProductionBranch();
        }

        private static async Task<ExitCode> ExecuteNuGetPackAsync(
            string nuGetExePath,
            string packagesDirectoryPath,
            ILogger logger,
            string nuSpecFileCopyPath,
            string properties,
            NuSpec nuSpecCopy,
            List<string> removedTags,
            bool keepBinaryAndSourcePackagesTogetherEnabled = false,
            bool nugetSymbolPackageEnabled = false,
            bool ignoreWarnings = false,
            CancellationToken cancellationToken = default)
        {
            bool hasRemovedNoSourceTag =
                removedTags.Any(
                    tag => tag.Equals(WellKnownNuGetTags.NoSource, StringComparison.InvariantCultureIgnoreCase));

            ExitCode result;
            try
            {
                var arguments = new List<string>
                {
                    "pack",
                    nuSpecFileCopyPath,
                    "-Properties",
                    properties,
                    "-OutputDirectory",
                    packagesDirectoryPath,
                    "-Version",
                    nuSpecCopy.Version
                };

                if (!hasRemovedNoSourceTag && nugetSymbolPackageEnabled)
                {
                    arguments.Add("-Symbols");
                }

                if (logger.IsEnabled(LogEventLevel.Verbose))
                {
                    arguments.Add("-Verbosity");
                    arguments.Add("Detailed");
                }

                if (ignoreWarnings)
                {
                    arguments.Add("-NoPackageAnalysis");
                }

                ExitCode processResult =
                    await
                        ProcessRunner.ExecuteAsync(
                            nuGetExePath,
                            arguments: arguments,
                            standardOutLog: logger.Information,
                            standardErrorAction: logger.Error,
                            toolAction: logger.Information,
                            cancellationToken: cancellationToken,
                            verboseAction: logger.Verbose,
                            debugAction: logger.Debug,
                            addProcessNameAsLogCategory: true,
                            addProcessRunnerCategory: true).ConfigureAwait(false);

                var packagesDirectory = new DirectoryInfo(packagesDirectoryPath);

                if (!keepBinaryAndSourcePackagesTogetherEnabled)
                {
                    logger.Information("The flag {NuGetKeepBinaryAndSymbolPackagesTogetherEnabled} is set to false, separating binary packages from symbol packages", WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled);
                    List<string> nugetPackages = packagesDirectory.GetFiles("*.nupkg", SearchOption.TopDirectoryOnly)
                        .Select(file => file.FullName)
                        .ToList();
                    List<string> nugetSymbolPackages = packagesDirectory
                        .GetFiles("*.symbols.nupkg", SearchOption.TopDirectoryOnly)
                        .Select(file => file.FullName)
                        .ToList();

                    List<string> binaryPackages = nugetPackages.Except(nugetSymbolPackages).ToList();

                    DirectoryInfo binaryPackagesDirectory =
                        new DirectoryInfo(Path.Combine(packagesDirectory.FullName, "binary")).EnsureExists();

                    DirectoryInfo symbolPackagesDirectory =
                        new DirectoryInfo(Path.Combine(packagesDirectory.FullName, "symbol")).EnsureExists();

                    foreach (string binaryPackage in binaryPackages)
                    {
                        var sourceFile = new FileInfo(binaryPackage);
                        var targetBinaryFile =
                            new FileInfo(Path.Combine(binaryPackagesDirectory.FullName, sourceFile.Name));

                        if (targetBinaryFile.Exists)
                        {
                            targetBinaryFile.Delete();
                        }

                        logger.Debug("Copying NuGet binary package '{BinaryPackage}' to '{TargetBinaryFile}'", binaryPackage, targetBinaryFile);
                        sourceFile.MoveTo(targetBinaryFile.FullName);
                    }

                    foreach (string sourcePackage in nugetSymbolPackages)
                    {
                        var sourceFile = new FileInfo(sourcePackage);
                        var targetSymbolFile =
                            new FileInfo(Path.Combine(symbolPackagesDirectory.FullName, sourceFile.Name));

                        if (targetSymbolFile.Exists)
                        {
                            targetSymbolFile.Delete();
                        }

                        logger.Debug("Copying NuGet symbol package '{SourcePackage}' to '{TargetSymbolFile}'", sourcePackage, targetSymbolFile);
                        sourceFile.MoveTo(targetSymbolFile.FullName);
                    }
                }

                result = processResult;
            }
            finally
            {
                if (File.Exists(nuSpecFileCopyPath))
                {
                    File.Delete(nuSpecFileCopyPath);
                }
            }

            return result;
        }
    }
}
