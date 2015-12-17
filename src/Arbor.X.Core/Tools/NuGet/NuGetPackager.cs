using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Extensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;

using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NuGetPackager
    {
        readonly ILogger _logger;

        public NuGetPackager(ILogger logger)
        {
            _logger = logger;
        }

        static string GetProperties(string configuration)
        {
            var propertyValues = new List<KeyValuePair<string, string>>
                                     {
                                         new KeyValuePair<string, string>(
                                             "configuration", configuration)
                                     };

            var formattedValues = propertyValues.Select(item => $"{item.Key}={item.Value}");
            string properties = string.Join(";", formattedValues);
            return properties;
        }

        static bool IsReleaseBuild(string releaseBuild, string branchName)
        {
            bool isReleaseBuild = releaseBuild.TryParseBool(defaultValue: false);

            return isReleaseBuild || branchName.Equals("master", StringComparison.InvariantCultureIgnoreCase);
        }

        public async Task<ExitCode> CreatePackageAsync(string packageSpecificationPath, NuGetPackageConfiguration packageConfiguration, bool ignoreWarnings = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.WriteDebug($"Using NuGet package configuration {packageConfiguration}");


            NuSpec nuSpec = NuSpec.Parse(packageSpecificationPath);

            var properties = GetProperties(packageConfiguration.Configuration);

            if (!string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
            {
                _logger.Write($"Using NuGet package id override '{packageConfiguration.PackageIdOverride}'");
            }

            string packageId = !string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride) ? packageConfiguration.PackageIdOverride : NuGetPackageIdHelper.CreateNugetPackageId(nuSpec.PackageId, packageConfiguration.IsReleaseBuild,
                packageConfiguration.BranchName, packageConfiguration.BranchNameEnabled);

            if (string.IsNullOrWhiteSpace(packageConfiguration.PackageIdOverride))
            {
                _logger.Write($"Using NuGet package ID {packageId}");
            }
            else
            {
                _logger.Write($"Using NuGet package version override '{packageConfiguration.PackageIdOverride}'");
            }

            string nuGetPackageVersion = !string.IsNullOrWhiteSpace(packageConfiguration.NuGetPackageVersionOverride) ? packageConfiguration.NuGetPackageVersionOverride : NuGetVersionHelper.GetVersion(packageConfiguration.Version, packageConfiguration.IsReleaseBuild, packageConfiguration.Suffix, packageConfiguration.BuildNumberEnabled, _logger);

            if (string.IsNullOrWhiteSpace(packageConfiguration.NuGetPackageVersionOverride))
            {
                _logger.Write($"Using NuGet package version {nuGetPackageVersion}");
            }
            else
            {
                _logger.Write($"Using NuGet package version override '{packageConfiguration.NuGetPackageVersionOverride}'");
            }

            var nuSpecInfo = new FileInfo(packageSpecificationPath);
            // ReSharper disable AssignNullToNotNullAttribute
            var nuSpecFileCopyPath = Path.Combine(nuSpecInfo.DirectoryName, $"{Guid.NewGuid()}-{nuSpecInfo.Name}");
            // ReSharper restore AssignNullToNotNullAttribute

            var nuSpecCopy = new NuSpec(packageId, nuGetPackageVersion, nuSpecInfo.FullName);

            var nuSpecTempDirectory = Path.Combine(packageConfiguration.TempPath, "nuget-specifications");

            if (!Directory.Exists(nuSpecTempDirectory))
            {
                Directory.CreateDirectory(nuSpecTempDirectory);
            }

            _logger.WriteVerbose($"Saving new nuspec '{nuSpecFileCopyPath}'");
            nuSpecCopy.Save(nuSpecFileCopyPath);

            List<string> removedTags = new List<string>();

            if (packageConfiguration.AllowManifestReWrite)
            {
                _logger.WriteVerbose($"Rewriting manifest in NuSpec '{nuSpecFileCopyPath}'");

                var manitestReWriter = new ManitestReWriter();
                ManifestReWriteResult manifestReWriteResult = manitestReWriter.Rewrite(nuSpecFileCopyPath);

                removedTags.AddRange(manifestReWriteResult.RemoveTags);
            }
            else
            {
                _logger.WriteVerbose("Rewriting manifest disabled");
            }

            _logger.WriteVerbose($"Created nuspec content: {Environment.NewLine}{File.ReadAllText(nuSpecFileCopyPath)}");

            var result = await ExecuteNuGetPackAsync(packageConfiguration.NuGetExePath, packageConfiguration.PackagesDirectory, _logger, nuSpecFileCopyPath, properties, nuSpecCopy, removedTags, cancellationToken: cancellationToken, ignoreWarnings: ignoreWarnings);

            return result;
        }

        static async Task<ExitCode> ExecuteNuGetPackAsync(string nuGetExePath, string packagesDirectoryPath, ILogger _logger, string nuSpecFileCopyPath, string properties, NuSpec nuSpecCopy, List<string> removedTags, bool keepBinaryAndSourcePackagesTogetherEnabled = false, bool nugetSymbolPackageEnabled = false, bool ignoreWarnings = false, CancellationToken cancellationToken = default (CancellationToken))
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

                if (LogLevel.Verbose.Level <= _logger.LogLevel.Level)
                {
                    arguments.Add("-Verbosity");
                    arguments.Add("Detailed");
                }

                if (ignoreWarnings)
                {
                    arguments.Add("-NoPackageAnalysis");
                }

                var processResult =
                    await
                    ProcessRunner.ExecuteAsync(nuGetExePath, arguments: arguments, standardOutLog: _logger.Write,
                        standardErrorAction: _logger.WriteError, toolAction: _logger.Write, cancellationToken: cancellationToken, verboseAction: _logger.WriteVerbose, debugAction: _logger.WriteDebug,
                    addProcessNameAsLogCategory: true,
                    addProcessRunnerCategory: true);

                var packagesDirectory = new DirectoryInfo(packagesDirectoryPath);

                if (!keepBinaryAndSourcePackagesTogetherEnabled)
                {
                    _logger.Write(
                        $"The flag {WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled} is set to false, separating binary packages from symbol packages");
                    var nugetPackages = packagesDirectory.GetFiles("*.nupkg", SearchOption.TopDirectoryOnly).Select(file => file.FullName).ToList();
                    var nugetSymbolPackages = packagesDirectory.GetFiles("*.symbols.nupkg", SearchOption.TopDirectoryOnly).Select(file => file.FullName).ToList();

                    var binaryPackages = nugetPackages.Except(nugetSymbolPackages).ToList();

                    var binaryPackagesDirectory =
                        new DirectoryInfo(Path.Combine(packagesDirectory.FullName, "binary")).EnsureExists();

                    var symbolPackagesDirectory = new DirectoryInfo(Path.Combine(packagesDirectory.FullName, "symbol")).EnsureExists();

                    foreach (var binaryPackage in binaryPackages)
                    {
                        var sourceFile = new FileInfo(binaryPackage);
                        var targetBinaryFile = new FileInfo(Path.Combine(binaryPackagesDirectory.FullName, sourceFile.Name));

                        if (targetBinaryFile.Exists)
                        {
                            targetBinaryFile.Delete();
                        }

                        _logger.WriteDebug($"Copying NuGet binary package '{binaryPackage}' to '{targetBinaryFile}'");
                        sourceFile.MoveTo(targetBinaryFile.FullName);
                    }

                    foreach (var sourcePackage in nugetSymbolPackages)
                    {
                        var sourceFile = new FileInfo(sourcePackage);
                        var targetSymbolFile = new FileInfo(Path.Combine(symbolPackagesDirectory.FullName, sourceFile.Name));

                        if (targetSymbolFile.Exists)
                        {
                            targetSymbolFile.Delete();
                        }

                        _logger.WriteDebug($"Copying NuGet symbol package '{sourcePackage}' to '{targetSymbolFile}'");
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

        public static NuGetPackageConfiguration GetNuGetPackageConfiguration(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string packagesDirectory,
            string vcsRootDir)
        {
            var version = buildVariables.Require(WellKnownVariables.Version).ThrowIfEmptyValue();
            var releaseBuild = buildVariables.Require(WellKnownVariables.ReleaseBuild).ThrowIfEmptyValue();
            var branchName = buildVariables.Require(WellKnownVariables.BranchLogicalName).ThrowIfEmptyValue();
            var configuration = buildVariables.Require(WellKnownVariables.Configuration).ThrowIfEmptyValue().Value;
            var tempDirectory = buildVariables.Require(WellKnownVariables.TempDirectory).ThrowIfEmptyValue();
            var nuGetExePath = buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath).ThrowIfEmptyValue().Value;

            var suffix = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageArtifactsSuffix, "build");
            var buildNumberEnabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.BuildNumberInNuGetPackageArtifactsEnabled,
                true);

            var keepBinaryAndSourcePackagesTogetherEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled, true);
            var branchNameEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageIdBranchNameEnabled, false);
            var packageIdOverride = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageIdOverride, null);
            var nuGetSymbolPackagesEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.NuGetSymbolPackagesEnabled, true);
            var nuGetPackageVersionOverride =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageVersionOverride, null);
            var allowManifestReWrite = buildVariables.GetBooleanByKey(WellKnownVariables.NuGetAllowManifestReWrite, false);

            var buildPackagesOnAnyBranch =
                buildVariables.GetBooleanByKey(WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled, false);

            if (!buildPackagesOnAnyBranch)
            {
                if (branchName.Value.Equals("master", StringComparison.InvariantCultureIgnoreCase))
                {
                    logger.WriteWarning(
                        $"NuGet package creation is not supported on 'master' branch. To force NuGet package creation, set environment variable '{WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled}' to value 'true'");
                    //return ExitCode.Success;
                }
            }
            else
            {
                logger.WriteVerbose(
                    $"Flag '{WellKnownVariables.NuGetCreatePackagesOnAnyBranchEnabled}' is set to true, creating NuGet packages");
            }

            bool isReleaseBuild = IsReleaseBuild(releaseBuild.Value, branchName.Value);

            logger.WriteVerbose(
                $"Based on branch {branchName.Value} and release build flags {releaseBuild.Value}, the build is considered {(isReleaseBuild ? "release" : "not  release")}");

            if (configuration.Equals("debug", StringComparison.InvariantCultureIgnoreCase) && isReleaseBuild)
            {
                logger.Write(
                    "The current configuration is 'debug' but the build indicates that this is a release build, using 'release' configuration instead");
                configuration = "release";
            }

            if (!Directory.Exists(packagesDirectory))
            {
                Directory.CreateDirectory(packagesDirectory);
            }

            if (!File.Exists(nuGetExePath))
            {
                logger.WriteError($"The NuGet.exe path {nuGetExePath} was not found or NuGet could not be downloaded");
                //return ExitCode.Failure;
            }

            logger.WriteVerbose($"Scanning directory '{vcsRootDir}' for .nuspec files");

            NuGetPackageConfiguration packageConfiguration = new NuGetPackageConfiguration(
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

    }

}