using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Defensive;
using Arbor.Processing;
using JetBrains.Annotations;
using NuGet.Versioning;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Arbor.Build.Core.Tools.NuGet
{
    [UsedImplicitly]
    public class NuGetPackager
    {
        private readonly BuildContext _buildContext;

        private readonly ILogger _logger;

        public const string SnupkgPackageFormat = "snupkg";

        public NuGetPackager(ILogger logger, BuildContext buildContext)
        {
            _logger = logger;
            _buildContext = buildContext;
        }

        public NuGetPackageConfiguration GetNuGetPackageConfiguration(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string packagesDirectory,
            string vcsRootDir,
            string packageNameSuffix)
        {
            logger ??= Logger.None;
            IVariable version = buildVariables.Require(WellKnownVariables.Version).ThrowIfEmptyValue();

            IVariable branchName = buildVariables.Require(WellKnownVariables.BranchLogicalName).ThrowIfEmptyValue();

            string? currentConfiguration = buildVariables.GetVariableValueOrDefault(WellKnownVariables.CurrentBuildConfiguration, null);

            string? staticConfiguration =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_BuildConfiguration,
                    null) ??
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.Configuration, null);

            string? buildConfiguration = currentConfiguration
                                        ?? staticConfiguration;

            IVariable tempDirectory = buildVariables.Require(WellKnownVariables.TempDirectory).ThrowIfEmptyValue();
            string nuGetExePath = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath)
                .ThrowIfEmptyValue()
                .Value;

            string? suffix =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageArtifactsSuffix, "build");

            bool buildNumberEnabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.BuildNumberInNuGetPackageArtifactsEnabled,
                true);

            bool keepBinaryAndSourcePackagesTogetherEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled,
                    true);

            bool branchNameEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageIdBranchNameEnabled, true);

            string? packageIdOverride =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageIdOverride, null);

            bool nuGetSymbolPackagesEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetSymbolPackagesEnabled);

            string? nuGetPackageVersionOverride =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageVersionOverride, null);

            bool allowManifestReWrite =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetAllowManifestReWrite);

            bool buildPackagesOnAnyBranch =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled);

            if (!buildPackagesOnAnyBranch)
            {
                if (branchName.Value.Equals("master", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warning(
                        "NuGet package creation is not supported on 'master' branch. To force NuGet package creation, set environment variable '{NuGetCreatePackagesOnAnyBranchEnabled}' to value 'true'",
                        WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled);

                    // return ExitCode.Success;
                }
            }
            else
            {
                logger.Verbose("Flag '{NuGetCreatePackagesOnAnyBranchEnabled}' is set to true, creating NuGet packages",
                    WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled);
            }

            string? packageFormat = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetSymbolPackageFormat, SnupkgPackageFormat);

            Maybe<BranchName> branchNameMayBe = BranchName.TryParse(branchName.Value);

            if (!branchNameMayBe.HasValue)
            {
                throw new InvalidOperationException(Resources.TheBranchNameCouldNotBeFound);
            }

            bool isReleaseBuild = IsStablePackage(branchNameMayBe.Value);

            string semVer = NuGetVersionHelper.GetVersion(
                version.Value,
                isReleaseBuild,
                "build",
                true,
                null,
                logger,
                NuGetVersioningSettings.Default);

            var semanticVersion = SemanticVersion.Parse(semVer);

            //logger.Verbose("Based on branch {Value} and release build flags {Value1}, the build is considered {V}",
            //    branchName.Value,
            //    releaseBuild.Value,
            //    isReleaseBuild ? WellKnownConfigurations.Release : "not release");

            if (!string.IsNullOrWhiteSpace(buildConfiguration)
                && buildConfiguration.Equals(WellKnownConfigurations.Debug, StringComparison.OrdinalIgnoreCase) && isReleaseBuild)
            {
                logger.Information(
                    "The current configuration is 'debug' but the build indicates that this is a release build, using 'release' configuration instead");
                buildConfiguration = WellKnownConfigurations.Release;
            }

            if (!Directory.Exists(packagesDirectory))
            {
                Directory.CreateDirectory(packagesDirectory);
            }

            if (!File.Exists(nuGetExePath))
            {
                logger.Error("The NuGet.exe path {NuGetExePath} was not found or NuGet could not be downloaded",
                    nuGetExePath);
            }

            logger.Verbose("Scanning directory '{VcsRootDir}' for .nuspec files", vcsRootDir);

            var packageConfiguration = new NuGetPackageConfiguration(
                buildConfiguration,
                semanticVersion,
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
                tempDirectory.Value,
                nuGetSymbolPackagesFormat: packageFormat,
                packageNameSuffix: packageNameSuffix);
            return packageConfiguration;
        }

        public async Task<ExitCode> CreatePackageAsync(
            [NotNull] string packageSpecificationPath,
            [NotNull] NuGetPackageConfiguration packageConfiguration,
            bool ignoreWarnings = false,
            CancellationToken cancellationToken = default)
        {
            if (packageConfiguration == null)
            {
                throw new ArgumentNullException(nameof(packageConfiguration));
            }

            if (string.IsNullOrWhiteSpace(packageSpecificationPath))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(packageSpecificationPath));
            }

            _logger.Debug("Using NuGet package configuration {PackageConfiguration}", packageConfiguration);

            NuSpec nuSpec = NuSpec.Parse(packageSpecificationPath);

            string properties = GetProperties(packageConfiguration.Configuration);

            if (!string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
            {
                _logger.Information("Using NuGet package id override '{PackageIdOverride}'",
                    packageConfiguration.PackageIdOverride);
            }

            string packageId = !string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride)
                ? packageConfiguration.PackageIdOverride
                : NuGetPackageIdHelper.CreateNugetPackageId(
                    nuSpec.PackageId,
                    packageConfiguration.IsReleaseBuild,
                    packageConfiguration.BranchName,
                    packageConfiguration.BranchNameEnabled,
                    packageConfiguration.PackageNameSuffix);

            if (string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
            {
                _logger.Information("Using NuGet package ID {PackageId}", packageId);
            }
            else
            {
                _logger.Information("Using NuGet package version override '{PackageIdOverride}'",
                    packageConfiguration.PackageIdOverride);
            }

            string nuGetPackageVersion = packageConfiguration.Version.ToNormalizedString();

            _logger.Information("{NuGetUsage}",
                string.IsNullOrWhiteSpace(packageConfiguration.NuGetPackageVersionOverride)
                    ? $"Using NuGet package version {nuGetPackageVersion}"
                    : $"Using NuGet package version override '{packageConfiguration.NuGetPackageVersionOverride}'");

            var nuSpecInfo = new FileInfo(packageSpecificationPath);

            // ReSharper disable AssignNullToNotNullAttribute
            string nuSpecFileCopyPath =
                Path.Combine(nuSpecInfo.DirectoryName, $"{packageId}-{DateTime.Now.Ticks}.nuspec");

            // ReSharper restore AssignNullToNotNullAttribute

            var nuSpecCopy = new NuSpec(packageId, packageConfiguration.Version, nuSpecInfo.FullName);

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

                var manifestReWriter = new ManitestReWriter();
                ManifestReWriteResult manifestReWriteResult = manifestReWriter.Rewrite(nuSpecFileCopyPath);

                removedTags.AddRange(manifestReWriteResult.RemoveTags);
            }
            else
            {
                _logger.Verbose("Rewriting manifest disabled");
            }

            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose("Created nuspec content: {NewLine}{PackageContent}",
                    Environment.NewLine,
                    GetNuSpecContent(nuSpecFileCopyPath));
            }

            ExitCode result = await ExecuteNuGetPackAsync(
                packageConfiguration.NuGetExePath,
                packageConfiguration.PackagesDirectory,
                _logger,
                nuSpecFileCopyPath,
                properties,
                nuSpecCopy,
                removedTags,
                cancellationToken: cancellationToken,
                nugetSymbolPackageEnabled: packageConfiguration.NuGetSymbolPackagesEnabled,
                symbolsFormat: packageConfiguration.NuGetSymbolPackagesFormat,
                ignoreWarnings: ignoreWarnings).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                string content = GetNuSpecContent(nuSpecFileCopyPath);
                _logger.Error("Could not create NuGet package from nuspec {NewLine}{NuSpec}",
                    Environment.NewLine,
                    content);
            }

            if (File.Exists(nuSpecFileCopyPath))
            {
                File.Delete(nuSpecFileCopyPath);
            }

            return result;
        }

        private static string GetNuSpecContent(string nuSpecFileCopyPath) => File.ReadAllText(nuSpecFileCopyPath);

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

        private bool IsStablePackage(BranchName branchName) =>
            (_buildContext.Configurations.Count == 1 &&
             _buildContext.Configurations.Single()
                 .Equals(WellKnownConfigurations.Release, StringComparison.OrdinalIgnoreCase))
            || branchName.IsProductionBranch();

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
            string symbolsFormat = "",
            CancellationToken cancellationToken = default)
        {
            bool hasRemovedNoSourceTag =
                removedTags.Any(
                    tag => tag.Equals(WellKnownNuGetTags.NoSource, StringComparison.OrdinalIgnoreCase));

            ExitCode result;
            var arguments = new List<string>
            {
                "pack",
                nuSpecFileCopyPath,
                "-Properties",
                properties,
                "-OutputDirectory",
                packagesDirectoryPath,
                "-Version",
                nuSpecCopy.Version.ToNormalizedString()
            };

            if (!hasRemovedNoSourceTag && nugetSymbolPackageEnabled)
            {
                arguments.Add("-Symbols");

                if (!string.IsNullOrWhiteSpace(symbolsFormat) && symbolsFormat.Equals(SnupkgPackageFormat, StringComparison.OrdinalIgnoreCase))
                {
                    arguments.Add("-SymbolPackageFormat");
                    arguments.Add(SnupkgPackageFormat);
                }
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
                    ProcessRunner.ExecuteProcessAsync(
                        nuGetExePath,
                        arguments: arguments,
                        standardOutLog: logger.Information,
                        standardErrorAction: logger.Error,
                        toolAction: logger.Information,
                        cancellationToken: cancellationToken,
                        verboseAction: logger.Verbose,
                        debugAction: logger.Debug).ConfigureAwait(false);

            var packagesDirectory = new DirectoryInfo(packagesDirectoryPath);

            if (!keepBinaryAndSourcePackagesTogetherEnabled)
            {
                logger.Information(
                    "The flag {NuGetKeepBinaryAndSymbolPackagesTogetherEnabled} is set to false, separating binary packages from symbol packages",
                    WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled);

                var nugetPackages = packagesDirectory.GetFiles("*.nupkg", SearchOption.TopDirectoryOnly)
                    .Select(file => file.FullName)
                    .ToList();

                var nugetSymbolPackages = packagesDirectory
                    .GetFiles("*.symbols.nupkg", SearchOption.TopDirectoryOnly)
                    .Select(file => file.FullName)
                    .ToList();

                var binaryPackages = nugetPackages.Except(nugetSymbolPackages).ToList();

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

                    logger.Debug("Copying NuGet binary package '{BinaryPackage}' to '{TargetBinaryFile}'",
                        binaryPackage,
                        targetBinaryFile);
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

                    logger.Debug("Copying NuGet symbol package '{SourcePackage}' to '{TargetSymbolFile}'",
                        sourcePackage,
                        targetSymbolFile);
                    sourceFile.MoveTo(targetSymbolFile.FullName);
                }
            }

            result = processResult;

            return result;
        }
    }
}
