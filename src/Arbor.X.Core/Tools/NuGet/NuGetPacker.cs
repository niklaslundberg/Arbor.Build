using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(650)]
    public class NuGetPacker : ITool
    {
        CancellationToken _cancellationToken;
        bool _keepBinaryAndSourcePackagesTogetherEnabled;

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            var enabled = buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageEnabled, defaultValue: true);

            if (!enabled)
            {
                logger.WriteWarning(string.Format("NuGet Packer is disabled (build variable '{0}' is set to false", WellKnownVariables.NuGetPackageEnabled));
                return ExitCode.Success;
            }

            var artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();
            var version = buildVariables.Require(WellKnownVariables.Version).ThrowIfEmptyValue();
            var releaseBuild = buildVariables.Require(WellKnownVariables.ReleaseBuild).ThrowIfEmptyValue();
            var branchName = buildVariables.Require(WellKnownVariables.BranchName).ThrowIfEmptyValue();
            var configuration = buildVariables.Require(WellKnownVariables.Configuration).ThrowIfEmptyValue().Value;
            var tempDirectory = buildVariables.Require(WellKnownVariables.TempDirectory).ThrowIfEmptyValue();
            var nuGetExePath =
                buildVariables.Require(WellKnownVariables.ExternalTools_NuGet_ExePath).ThrowIfEmptyValue().Value;

            var suffix = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageArtifactsSuffix, "build");
            var enableBuildNumber = buildVariables.GetBooleanByKey(WellKnownVariables.BuildNumberInNuGetPackageArtifactsEnabled, true);

            _keepBinaryAndSourcePackagesTogetherEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled, true);

            if (branchName.Value.Equals("master", StringComparison.InvariantCultureIgnoreCase))
            {
                logger.WriteWarning("NuGet package creation is not supported on 'master' branch");
                return ExitCode.Success;
            }

            var isReleaseBuild = IsReleaseBuild(releaseBuild);

            var packagesDirectory = Path.Combine(artifacts.Value, "packages");

            if (!Directory.Exists(packagesDirectory))
            {
                Directory.CreateDirectory(packagesDirectory);
            }

            if (!File.Exists(nuGetExePath))
            {
                logger.WriteError(string.Format(
                    "The NuGet.exe path {0} was not found or NuGet could not be downloaded", nuGetExePath));
                return ExitCode.Failure;
            }

            var vcsRootDir = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            logger.WriteVerbose(string.Format("Scanning directory '{0}' for .nuspec files", vcsRootDir));

            var packageDirectory = PackageDirectory();

            var packageSpecifications = GetPackageSpecifications(logger, vcsRootDir, packageDirectory);

            var result = await ProcessPackagesAsync(packageSpecifications, nuGetExePath, packagesDirectory, version, isReleaseBuild, configuration, branchName, logger, tempDirectory, suffix, enableBuildNumber);

            return result;
        }

        static IEnumerable<string> GetPackageSpecifications(ILogger logger, string vcsRootDir, string packageDirectory)
        {
            var packageSpecifications = Directory.GetFiles(vcsRootDir, "*.nuspec", SearchOption.AllDirectories)
                                                 .Where(
                                                     file =>
                                                     file.IndexOf(packageDirectory, StringComparison.Ordinal) < 0)
                                                 .ToList();

            logger.WriteVerbose(string.Format("Found nuspec files [{0}]: {1}{2}", packageSpecifications.Count,
                                       Environment.NewLine, string.Join(Environment.NewLine, packageSpecifications)));
            return packageSpecifications;
        }

        static string PackageDirectory()
        {
            var packageDirectory = string.Format("{0}packages{0}", Path.DirectorySeparatorChar);
            return packageDirectory;
        }

        static bool IsReleaseBuild(IVariable releaseBuild)
        {
            bool isReleaseBuild = releaseBuild.Value.TryParseBool(defaultValue:false);

            return isReleaseBuild;
        }

        async Task<ExitCode> ProcessPackagesAsync(IEnumerable<string> packageSpecifications, string nuGetExePath, string packagesDirectory, IVariable version, bool isReleaseBuild, string configuration, IVariable branchName, ILogger logger, IVariable tempDirectory, string suffix, bool enableBuildNumber)
        {
            foreach (var packageSpecification in packageSpecifications)
            {
                var packageResult =
                    await
                        CreatePackageAsync(nuGetExePath, packageSpecification, packagesDirectory, version,
                            isReleaseBuild,
                            configuration, branchName, logger, tempDirectory, suffix, enableBuildNumber);

                if (!packageResult.IsSuccess)
                {
                    logger.WriteError(string.Format("Could not create NuGet package from specification '{0}'",
                        packageSpecification));
                    return packageResult;
                }
            }

            return ExitCode.Success;
        }

        async Task<ExitCode> CreatePackageAsync(string nuGetExePath, string nuspecFilePath, string packagesDirectory, IVariable version, bool isReleaseBuild, string configuration, IVariable branchName, ILogger logger, IVariable tempDirectory, string suffix, bool enableBuildNumber)
        {
            NuSpec nuSpec = NuSpec.Parse(nuspecFilePath);

            var properties = GetProperties(configuration);
            
            string packageId = NuGetPackageIdHelper.CreateNugetPackageId(nuSpec.PackageId, isReleaseBuild,
                                                                         branchName.Value);

            string nuGetPackageVersion = NuGetVersionHelper.GetVersion(version.Value, isReleaseBuild, suffix, enableBuildNumber);
            
            var nuSpecInfo = new FileInfo(nuspecFilePath);
// ReSharper disable AssignNullToNotNullAttribute
            var nuSpecFileCopyPath = Path.Combine(nuSpecInfo.DirectoryName,
                                                  string.Format("{0}-{1}", Guid.NewGuid(), nuSpecInfo.Name));
// ReSharper restore AssignNullToNotNullAttribute

            var nuSpecCopy = new NuSpec(packageId, nuGetPackageVersion, nuSpecInfo.FullName);

            var nuSpecTempDirectory = Path.Combine(tempDirectory.Value, "nuget-specifications");

            if (!Directory.Exists(nuSpecTempDirectory))
            {
                Directory.CreateDirectory(nuSpecTempDirectory);
            }

            logger.WriteVerbose(string.Format("Saving new nuspec {0}", nuSpecFileCopyPath));
            nuSpecCopy.Save(nuSpecFileCopyPath);

            logger.WriteVerbose(string.Format("Created nuspec content: {0}{1}", Environment.NewLine, File.ReadAllText(nuSpecFileCopyPath)));

            var result = await ExecuteNuGetPackAsync(nuGetExePath, packagesDirectory, logger, nuSpecFileCopyPath, properties, nuSpecCopy);

            return result;
        }
        
        async Task<ExitCode> ExecuteNuGetPackAsync(string nuGetExePath, string packagesDirectoryPath, ILogger logger,
                                                string nuSpecFileCopyPath, string properties, NuSpec nuSpecCopy)
        {
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
                                        nuSpecCopy.Version,
                                        "-Symbols"
                                    };

                if (LogLevel.Verbose.Level <= logger.LogLevel.Level)
                {
                    arguments.Add("-Verbosity");
                    arguments.Add("Detailed");
                }

                var processResult =
                    await
                    ProcessRunner.ExecuteAsync(nuGetExePath, arguments: arguments, standardOutLog: logger.Write,
                                               standardErrorAction: logger.WriteError, toolAction: logger.Write, cancellationToken: _cancellationToken, verboseAction: logger.WriteVerbose);

                var packagesDirectory = new DirectoryInfo(packagesDirectoryPath);

                if (!_keepBinaryAndSourcePackagesTogetherEnabled)
                {
                    logger.Write(string.Format("The flag {0} is set to false, separating binary packages from symbol packages", WellKnownVariables.NuGetKeepBinaryAndSymbolPackagesTogetherEnabled));
                    var nugetPackages = packagesDirectory.GetFiles("*.nupkg", SearchOption.TopDirectoryOnly).Select(file => file.FullName).ToList();
                    var nugetSymbolPackages = packagesDirectory.GetFiles("*.symbols.nupkg", SearchOption.TopDirectoryOnly).Select(file => file.FullName).ToList();

                    var binaryPackages = nugetPackages.Except(nugetSymbolPackages).ToList();

                    var binaryPackagesDirectory =
                        new DirectoryInfo(Path.Combine(packagesDirectory.FullName, "binary")).EnsureExists();

                    var symbolPackagesDirectory = new DirectoryInfo(Path.Combine(packagesDirectory.FullName, "symbol")).EnsureExists();

                    foreach (var binaryPackage in binaryPackages)
                    {
                        var sourceFile = new FileInfo(binaryPackage);
                        var targetBinaryFile = Path.Combine(binaryPackagesDirectory.FullName, sourceFile.Name);

                        logger.WriteDebug(string.Format("Copying NuGet binary package '{0}' to '{1}'", binaryPackage, targetBinaryFile));
                        sourceFile.MoveTo(targetBinaryFile);
                    }

                    foreach (var sourcePackage in nugetSymbolPackages)
                    {
                        var sourceFile = new FileInfo(sourcePackage);
                        var targetSymbolFile = Path.Combine(symbolPackagesDirectory.FullName, sourceFile.Name);
                        logger.WriteDebug(string.Format("Copying NuGet symbol package '{0}' to '{1}'", sourcePackage, targetSymbolFile));
                        sourceFile.MoveTo(targetSymbolFile);
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

        static string GetProperties(string configuration)
        {
            var propertyValues = new List<KeyValuePair<string, string>>
                                     {
                                         new KeyValuePair<string, string>(
                                             "configuration", configuration)
                                     };

            var formattedValues = propertyValues.Select(item => string.Format("{0}={1}", item.Key, item.Value));
            string properties = string.Join(";", formattedValues);
            return properties;
        }
    }
}