using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Git;
using Arbor.Processing.Core;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Kudu
{
    [Priority(1100)]
    [UsedImplicitly]
    public class KuduDeploy : ITool
    {
        private string _artifacts;
        private bool _clearTarget;
        private bool _deleteExistingAppOfflineHtm;
        private BranchName _deployBranch;
        private string _deploymentTargetDirectory;
        private bool _excludeAppData;
        private string _ignoreDeleteDirectories;
        private string _ignoreDeleteFiles;
        private string _kuduConfigurationFallback;
        private bool _kuduEnabled;
        private bool _useAppOfflineFile;
        private string _vcsRoot;

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _kuduEnabled = buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Enabled) &&
                           bool.Parse(buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Enabled).Value);
            if (!_kuduEnabled)
            {
                return ExitCode.Success;
            }

            _vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            _artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;
            buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Platform).ThrowIfEmptyValue();
            _deployBranch = new BranchName(buildVariables
                .Require(WellKnownVariables.ExternalTools_Kudu_DeploymentBranchName)
                .Value);
            _deploymentTargetDirectory =
                buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_DeploymentTarget).Value;

            _kuduConfigurationFallback = buildVariables.HasKey(WellKnownVariables.KuduConfigurationFallback)
                ? buildVariables.Require(WellKnownVariables.KuduConfigurationFallback).Value
                : string.Empty;

            _clearTarget = buildVariables.GetBooleanByKey(WellKnownVariables.KuduClearFilesAndDirectories, false);
            _useAppOfflineFile = buildVariables.GetBooleanByKey(WellKnownVariables.KuduUseAppOfflineHtmFile, false);
            _excludeAppData = buildVariables.GetBooleanByKey(WellKnownVariables.KuduExcludeDeleteAppData, true);
            _deleteExistingAppOfflineHtm =
                buildVariables.GetBooleanByKey(WellKnownVariables.KuduDeleteExistingAppOfflineHtmFile, true);
            _ignoreDeleteFiles =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.KuduIgnoreDeleteFiles, string.Empty);
            _ignoreDeleteDirectories =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.KuduIgnoreDeleteDirectories, string.Empty);

            string branchNameOverride =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_Kudu_DeploymentBranchNameOverride,
                    string.Empty);

            if (!string.IsNullOrWhiteSpace(branchNameOverride))
            {
                logger.Information(
                    "Using branch name override '{BranchNameOverride}' instead of branch name '{_deployBranch}'",
                    branchNameOverride,
                    _deployBranch);
                _deployBranch = new BranchName(branchNameOverride);
            }

            var websitesDirectory = new DirectoryInfo(Path.Combine(_artifacts, "Websites"));

            if (!websitesDirectory.Exists)
            {
                logger.Information("No websites found. Ignoring Kudu deployment.");
                return ExitCode.Success;
            }

            DirectoryInfo[] builtWebsites = websitesDirectory.GetDirectories();

            if (builtWebsites.Length == 0)
            {
                logger.Information("No websites found. Ignoring Kudu deployment.");
                return ExitCode.Success;
            }

            DirectoryInfo websiteToDeploy;

            if (builtWebsites.Length == 1)
            {
                websiteToDeploy = builtWebsites.Single();
            }
            else
            {
                string siteToDeploy =
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.KuduSiteToDeploy, string.Empty);
                if (!string.IsNullOrWhiteSpace(siteToDeploy))
                {
                    DirectoryInfo foundDir = builtWebsites.SingleOrDefault(
                        dir => dir.Name.Equals(siteToDeploy, StringComparison.OrdinalIgnoreCase));

                    if (foundDir == null)
                    {
                        logger.Error(
                            "Found {Length} websites. Kudu deployment is specified for site {SiteToDeploy} but it was not found",
                            builtWebsites.Length,
                            siteToDeploy);
                        return ExitCode.Failure;
                    }

                    websiteToDeploy = foundDir;
                }
                else
                {
                    logger.Error(
                        "Found {Length} websites. Kudu deployment is only supported with a single website. \r\nBuilt websites: {V}. You can use variable '{KuduSiteToDeploy}' to specify a single website to be built",
                        builtWebsites.Length,
                        string.Join(Environment.NewLine, builtWebsites.Select(dir => dir.Name)),
                        WellKnownVariables.KuduSiteToDeploy);
                    return ExitCode.Failure;
                }
            }

            if (websiteToDeploy.GetDirectories().Length == 0)
            {
                logger.Error("Could not find any platform for website {Name}", websiteToDeploy.Name);
                return ExitCode.Failure;
            }

            if (websiteToDeploy.GetDirectories().Length > 1)
            {
                logger.Error("Could not find exactly one platform for website {Name}", websiteToDeploy.Name);
                return ExitCode.Failure;
            }

            DirectoryInfo platform = GetPlatform(websiteToDeploy);

            if (platform.GetDirectories().Length == 0)
            {
                logger.Error("Could not find any configuration for website {Name}", websiteToDeploy.Name);
                return ExitCode.Failure;
            }

            DirectoryInfo configuration = GetConfigurationDirectory(platform, logger);

            if (configuration == null)
            {
                logger.Error("No configuration for Kudu");
                return ExitCode.Failure;
            }

            string appOfflinePath = Path.Combine(_deploymentTargetDirectory, "app_offline.htm");

            logger.Information(
                "___________________ Kudu deploy ___________________ \r\nDeploying website {Name}, platform {Name1}, configuration {Name2}",
                websiteToDeploy.Name,
                platform.Name,
                configuration.Name);
            try
            {
                if (_useAppOfflineFile)
                {
                    logger.Verbose("Flag '{KuduUseAppOfflineHtmFile}' is set",
                        WellKnownVariables.KuduUseAppOfflineHtmFile);
                    try
                    {
                        using (var fs = new FileStream(appOfflinePath, FileMode.Create, FileAccess.Write))
                        {
                            using (var streamWriter = new StreamWriter(fs, Encoding.UTF8))
                            {
                                streamWriter.WriteLine(
                                    "File created by Arbor.X Kudu at {0} (UTC)",
                                    DateTime.UtcNow.ToString("O"));
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        logger.Warning(ex,
                            "Could not create app_offline.htm file in '{_deploymentTargetDirectory}', {Ex}",
                            _deploymentTargetDirectory);
                    }
                    catch (IOException ex)
                    {
                        logger.Warning(ex,
                            "Could not create app_offline.htm file in '{_deploymentTargetDirectory}', {Ex}",
                            _deploymentTargetDirectory);
                    }
                }
                else
                {
                    logger.Verbose("Flag '{KuduUseAppOfflineHtmFile}' is not set",
                        WellKnownVariables.KuduUseAppOfflineHtmFile);
                }

                if (_clearTarget)
                {
                    logger.Verbose("Flag '{KuduClearFilesAndDirectories}' is set",
                        WellKnownVariables.KuduClearFilesAndDirectories);
                    logger.Information("Removing files and directories from target '{_deploymentTargetDirectory}'",
                        _deploymentTargetDirectory);
                    try
                    {
                        var directoryFilters = new List<string>();

                        if (_excludeAppData)
                        {
                            directoryFilters.Add("App_Data");
                        }

                        string[] customFileExcludes = GetExcludes(_ignoreDeleteFiles).ToArray();
                        string[] customDirectoryExcludes = GetExcludes(_ignoreDeleteDirectories).ToArray();

                        var fileFilters = new List<string>();

                        if (_useAppOfflineFile || !_deleteExistingAppOfflineHtm)
                        {
                            fileFilters.Add("app_offline.htm");
                        }

                        if (customDirectoryExcludes.Length > 0)
                        {
                            logger.Verbose("Adding directory ignore patterns {V}",
                                string.Join(
                                    "|",
                                    $"'{customDirectoryExcludes.Select(item => (object)item)}'"));
                        }

                        if (customFileExcludes.Length > 0)
                        {
                            logger.Verbose("Adding file ignore patterns {V}",
                                string.Join(
                                    "|",
                                    $"'{customFileExcludes.Select(item => (object)item)}'"));
                        }

                        directoryFilters.AddRange(customDirectoryExcludes);
                        fileFilters.AddRange(customFileExcludes);

                        var deleter = new DirectoryDelete(directoryFilters, fileFilters, logger);

                        deleter.Delete(_deploymentTargetDirectory, false, true);
                    }
                    catch (IOException ex)
                    {
                        logger.Warning(ex,
                            "Could not clear all files and directories from target '{_deploymentTargetDirectory}', {Ex}",
                            _deploymentTargetDirectory);
                    }
                }
                else
                {
                    logger.Verbose(
                        "Flag '{KuduClearFilesAndDirectories}' is not set, skipping deleting files and directories from target '{_deploymentTargetDirectory}'",
                        WellKnownVariables.KuduClearFilesAndDirectories,
                        _deploymentTargetDirectory);
                }

                logger.Information("Copying files and directories from '{FullName}' to '{_deploymentTargetDirectory}'",
                    configuration.FullName,
                    _deploymentTargetDirectory);

                try
                {
                    ExitCode exitCode = await DirectoryCopy.CopyAsync(
                        configuration.FullName,
                        _deploymentTargetDirectory,
                        logger,
                        rootDir: _vcsRoot,
                        pathLookupSpecificationOption: new PathLookupSpecification()).ConfigureAwait(false);

                    if (!exitCode.IsSuccess)
                    {
                        return exitCode;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Kudu deploy could not copy files {Ex}");
                    return ExitCode.Failure;
                }
            }
            finally
            {
                if (_useAppOfflineFile || _deleteExistingAppOfflineHtm)
                {
                    if (File.Exists(appOfflinePath))
                    {
                        try
                        {
                            File.Delete(appOfflinePath);
                        }
                        catch (IOException ex)
                        {
                            logger.Warning(ex,
                                "Could not delete app_offline.htm file in '{_deploymentTargetDirectory}', {Ex}",
                                _deploymentTargetDirectory);
                        }
                    }
                }
            }

            return ExitCode.Success;
        }

        private IEnumerable<string> GetExcludes(string ignores)
        {
            if (string.IsNullOrWhiteSpace(ignores))
            {
                yield break;
            }

            string[] splitted = ignores.Split('|');

            foreach (string item in splitted)
            {
                yield return item;
            }
        }

        private DirectoryInfo GetConfigurationDirectory(DirectoryInfo platformDirectory, ILogger logger)
        {
            DirectoryInfo[] directoryInfos = platformDirectory.GetDirectories();

            if (directoryInfos.Length == 1)
            {
                DirectoryInfo directoryInfo = directoryInfos.Single();
                logger.Information("Found only one configuration: {Name}", directoryInfo.Name);
                return directoryInfo;
            }

            if (_deployBranch.IsProductionBranch())
            {
                logger.Information("Using deployment branch name {_deployBranch}", _deployBranch);

                DirectoryInfo productionConfig =
                    directoryInfos.SingleOrDefault(
                        di => di.Name.Equals("production", StringComparison.OrdinalIgnoreCase));

                if (productionConfig != null)
                {
                    logger.Information("On master or release branch, using {Name} configuration",
                        productionConfig.Name);
                    return productionConfig;
                }

                DirectoryInfo releaseConfig =
                    directoryInfos.SingleOrDefault(
                        di => di.Name.Equals("release", StringComparison.OrdinalIgnoreCase));

                if (releaseConfig != null)
                {
                    logger.Information("On master or release branch, using {Name} configuration", releaseConfig.Name);
                    return releaseConfig;
                }
            }
            else if (_deployBranch.IsDevelopBranch())
            {
                DirectoryInfo developConfig =
                    directoryInfos.SingleOrDefault(
                        di => di.Name.Equals("develop", StringComparison.OrdinalIgnoreCase) ||
                              di.Name.Equals("dev", StringComparison.OrdinalIgnoreCase));

                if (developConfig != null)
                {
                    logger.Information("On develop branch, using {Name} configuration", developConfig.Name);
                    return developConfig;
                }

                DirectoryInfo debugConfig =
                    directoryInfos.SingleOrDefault(
                        di => di.Name.Equals("debug", StringComparison.OrdinalIgnoreCase));

                if (debugConfig != null)
                {
                    logger.Information("On develop branch, using {Name} configuration", debugConfig.Name);
                    return debugConfig;
                }
            }
            else if (!string.IsNullOrWhiteSpace(_kuduConfigurationFallback))
            {
                DirectoryInfo configDir = directoryInfos.SingleOrDefault(
                    dir => dir.Name.Equals(_kuduConfigurationFallback, StringComparison.OrdinalIgnoreCase));

                logger.Information("Kudu fallback is '{_kuduConfigurationFallback}'", _kuduConfigurationFallback);

                if (configDir != null)
                {
                    logger.Information("Using Kudu fallback configuration {Name}", configDir.Name);

                    return configDir;
                }

                logger.Warning("Kudu fallback configuration '{_kuduConfigurationFallback}' was not found",
                    _kuduConfigurationFallback);
            }

            logger.Error("Could not determine Kudu deployment configuration: [{V}]",
                string.Join(", ", directoryInfos.Select(di => di.Name)));
            return null;
        }

        private DirectoryInfo GetPlatform(DirectoryInfo websiteToDeploy)
        {
            return websiteToDeploy.GetDirectories().Single();
        }
    }
}
