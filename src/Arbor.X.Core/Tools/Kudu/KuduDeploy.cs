using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Git;

namespace Arbor.X.Core.Tools.Kudu
{
    [Priority(1100)]
    public class KuduDeploy : ITool
    {
        string _artifacts;
        BranchName _deployBranch;
        string _deploymentTargetDirectory;
        bool _kuduEnabled;
        string _platform;
        string _kuduConfigurationFallback;
        bool _clearTarget;
        bool _useAppOfflineFile;
        bool _excludeAppData;
        bool _deleteExistingAppOfflineHtm;
        string _ignoreDeleteFiles;
        string _ignoreDeleteDirectories;
        private string _vcsRoot;

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            _kuduEnabled = buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Enabled) && bool.Parse(buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Enabled).Value);
            if (!_kuduEnabled)
            {
                return ExitCode.Success;
            }
            _vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            _artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;
            _platform = buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Platform).ThrowIfEmptyValue().Value;
            _deployBranch = new BranchName(buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_DeploymentBranchName).Value);
            _deploymentTargetDirectory =
                buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_DeploymentTarget).Value;

            _kuduConfigurationFallback = buildVariables.HasKey(WellKnownVariables.KuduConfigurationFallback)
                ? buildVariables.Require(WellKnownVariables.KuduConfigurationFallback).Value
                : "";

            _clearTarget = buildVariables.GetBooleanByKey(WellKnownVariables.KuduClearFilesAndDirectories, false);
            _useAppOfflineFile = buildVariables.GetBooleanByKey(WellKnownVariables.KuduUseAppOfflineHtmFile, false);
            _excludeAppData = buildVariables.GetBooleanByKey(WellKnownVariables.KuduExcludeDeleteAppData, true);
            _deleteExistingAppOfflineHtm = buildVariables.GetBooleanByKey(WellKnownVariables.KuduDeleteExistingAppOfflineHtmFile, true);
            _ignoreDeleteFiles = buildVariables.GetVariableValueOrDefault(WellKnownVariables.KuduIgnoreDeleteFiles, "");
            _ignoreDeleteDirectories = buildVariables.GetVariableValueOrDefault(WellKnownVariables.KuduIgnoreDeleteDirectories, "");

            var branchNameOverride = buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_Kudu_DeploymentBranchNameOverride, defaultValue: "");

            if (!string.IsNullOrWhiteSpace(branchNameOverride))
            {
                logger.Write(string.Format("Using branch name override '{0}' instead of branch name '{1}'", branchNameOverride, _deployBranch));
                _deployBranch = new BranchName(branchNameOverride);
            }

            var websitesDirectory = new DirectoryInfo(Path.Combine(_artifacts, "Websites"));

            if (!websitesDirectory.Exists)
            {
                logger.Write("No websites found. Ignoring Kudu deployment.");
                return ExitCode.Success;
            }

            var builtWebsites = websitesDirectory.GetDirectories();

            if (!builtWebsites.Any())
            {
                logger.Write("No websites found. Ignoring Kudu deployment.");
                return ExitCode.Success;
            }

            if (builtWebsites.Count() > 1)
            {
                logger.WriteError(
                    string.Format(
                        "Found {0} websites. Kudu deployment is only supported with a single website. \r\nBuilt websites: {1}",
                        builtWebsites.Count(), string.Join(Environment.NewLine, builtWebsites.Select(dir => dir.Name))));

                return ExitCode.Failure;
            }

            var websiteToDeploy = builtWebsites.Single();

            if (!websiteToDeploy.GetDirectories().Any())
            {
                logger.WriteError(string.Format("Could not find any platform for website {0}", websiteToDeploy.Name));
                return ExitCode.Failure;
            }

            if (websiteToDeploy.GetDirectories().Count() > 1)
            {
                logger.WriteError(string.Format("Could not find exactly one platform for website {0}", websiteToDeploy.Name));
                return ExitCode.Failure;
            }

            var platform = GetPlatform(websiteToDeploy);

            if (!platform.GetDirectories().Any())
            {
                logger.WriteError(string.Format("Could not find any configuration for website {0}", websiteToDeploy.Name));
                return ExitCode.Failure;
            }

            DirectoryInfo configuration = GetConfigurationDirectory(platform, logger);

            if (configuration == null)
            {
                logger.WriteError("No configuration for Kudu");
                return ExitCode.Failure;
            }

            var appOfflinePath = Path.Combine(_deploymentTargetDirectory, "app_offline.htm");

            logger.Write(string.Format("___________________ Kudu deploy ___________________ \r\nDeploying website {0}, platform {1}, configuration {2}", websiteToDeploy.Name, platform.Name, configuration.Name));
            try
            {
                if (_useAppOfflineFile)
                {
                    logger.WriteVerbose(string.Format("Flag '{0}' is set", WellKnownVariables.KuduUseAppOfflineHtmFile));
                    try
                    {
                        using (var fs = new FileStream(appOfflinePath, FileMode.Create, FileAccess.Write))
                        {
                            using (var streamWriter = new StreamWriter(fs, Encoding.UTF8))
                            {
                                streamWriter.WriteLine("File created by Arbor.X Kudu at {0} (UTC)",
                                    DateTime.UtcNow.ToString("O"));
                            }
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        logger.WriteWarning(string.Format("Could not create app_offline.htm file in '{0}', {1}",
                            _deploymentTargetDirectory, ex));
                    }
                    catch (IOException ex)
                    {
                        logger.WriteWarning(string.Format("Could not create app_offline.htm file in '{0}', {1}",
                            _deploymentTargetDirectory, ex));
                    }
                }
                else
                {
                    logger.WriteVerbose(string.Format("Flag '{0}' is not set", WellKnownVariables.KuduUseAppOfflineHtmFile));
                }

                if (_clearTarget)
                {
                    logger.WriteVerbose(string.Format("Flag '{0}' is set", WellKnownVariables.KuduClearFilesAndDirectories));
                    logger.Write(string.Format("Removing files and directories from target '{0}'",
                        _deploymentTargetDirectory));
                    try
                    {
                        var directoryFilters = new List<string>();

                        if (_excludeAppData)
                        {
                            directoryFilters.Add("App_Data");
                        }

                        var customFileExcludes = GetExcludes(_ignoreDeleteFiles).ToArray();
                        var customDirectoryExcludes = GetExcludes(_ignoreDeleteDirectories).ToArray();
                        
                        var fileFilters = new List<string>();

                        if (_useAppOfflineFile || !_deleteExistingAppOfflineHtm)
                        {
                            fileFilters.Add("app_offline.htm");
                        }

                        if (customDirectoryExcludes.Any())
                        {
                            logger.WriteVerbose("Adding directory ignore patterns " + string.Join("|", string.Format("'{0}'", customDirectoryExcludes.Select(item => (object)item))));
                        }

                        if (customFileExcludes.Any())
                        {
                            logger.WriteVerbose("Adding file ignore patterns " + string.Join("|", string.Format("'{0}'", customFileExcludes.Select(item => (object)item))));
                        }

                        directoryFilters.AddRange(customDirectoryExcludes);
                        fileFilters.AddRange(customFileExcludes);

                        var deleter = new DirectoryDelete(directoryFilters, fileFilters, logger);

                        deleter.Delete(_deploymentTargetDirectory, deleteSelf: false, deleteSelfFiles: true);
                    }
                    catch (IOException ex)
                    {
                        logger.WriteWarning(
                            string.Format("Could not clear all files and directories from target '{0}', {1}",
                                _deploymentTargetDirectory, ex));
                    }
                }
                else
                {
                    logger.WriteVerbose(string.Format("Flag '{0}' is not set, skipping deleting files and directories from target '{1}'", WellKnownVariables.KuduClearFilesAndDirectories, _deploymentTargetDirectory));
                }

                logger.Write(string.Format("Copying files and directories from '{0}' to '{1}'", configuration.FullName,
                    _deploymentTargetDirectory));
                
                try
                {
                    var exitCode = await DirectoryCopy.CopyAsync(configuration.FullName, _deploymentTargetDirectory, logger, rootDir: _vcsRoot);

                    if (!exitCode.IsSuccess)
                    {
                        return exitCode;
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteError(string.Format("Kudu deploy could not copy files {0}", ex));
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
                            logger.WriteWarning(string.Format("Could not delete app_offline.htm file in '{0}', {1}",
                                _deploymentTargetDirectory, ex));
                        }
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken); //TODO temp to avoid compiler warning

            return ExitCode.Success;
        }

        IEnumerable<string> GetExcludes(string ignores)
        {
            if (string.IsNullOrWhiteSpace(ignores))
            {
               yield break;
            }

            var splitted = ignores.Split(new[] {'|'});

            foreach (var item in splitted)
            {
                yield return item;
            }
        }

        DirectoryInfo GetConfigurationDirectory(DirectoryInfo platformDirectory, ILogger logger)
        {
            DirectoryInfo[] directoryInfos = platformDirectory.GetDirectories();

            if (directoryInfos.Count() == 1)
            {
                var directoryInfo = directoryInfos.Single();
                logger.Write("Found only one configuration: " + directoryInfo.Name);
                return directoryInfo;
            }

            if (_deployBranch.IsProductionBranch())
            {
                logger.Write(string.Format("Using deployment branch name {0}", _deployBranch));

                DirectoryInfo productionConfig =
                    directoryInfos.SingleOrDefault(
                        di => di.Name.Equals("production", StringComparison.InvariantCultureIgnoreCase));

                if (productionConfig != null)
                {
                    logger.Write(string.Format("On master or release branch, using {0} configuration",
                        productionConfig.Name));
                    return productionConfig;
                }

                DirectoryInfo releaseConfig =
                    directoryInfos.SingleOrDefault(
                        di => di.Name.Equals("release", StringComparison.InvariantCultureIgnoreCase));

                if (releaseConfig != null)
                {
                    logger.Write(string.Format("On master or release branch, using {0} configuration",
                        releaseConfig.Name));
                    return releaseConfig;
                }
            }
            else if (_deployBranch.IsDevelopBranch())
            {
                DirectoryInfo developConfig =
                    directoryInfos.SingleOrDefault(
                        di => di.Name.Equals("develop", StringComparison.InvariantCultureIgnoreCase) || di.Name.Equals("dev", StringComparison.InvariantCultureIgnoreCase));

                if (developConfig != null)
                {
                    logger.Write(string.Format("On develop branch, using {0} configuration",
                        developConfig.Name));
                    return developConfig;
                }

                DirectoryInfo debugConfig =
                    directoryInfos.SingleOrDefault(
                        di => di.Name.Equals("debug", StringComparison.InvariantCultureIgnoreCase));

                if (debugConfig != null)
                {
                    logger.Write(string.Format("On develop branch, using {0} configuration",
                        debugConfig.Name));
                    return debugConfig;
                }
            }
            else if (!string.IsNullOrWhiteSpace(_kuduConfigurationFallback))
            {
                var configDir = directoryInfos.SingleOrDefault(
                    dir => dir.Name.Equals(_kuduConfigurationFallback, StringComparison.InvariantCultureIgnoreCase));

                logger.Write(string.Format("Kudu fallback is '{0}'", _kuduConfigurationFallback));

                if (configDir != null)
                {
                    logger.Write(string.Format("Using Kudu fallback configuration {0}",
                        configDir.Name));

                    return configDir;
                }
                logger.WriteWarning(string.Format("Kudu fallback configuration '{0}' was not found", _kuduConfigurationFallback));
            }

            logger.WriteError(string.Format("Could not determine Kudu deployment configuration: [{0}]",
                string.Join(", ", directoryInfos.Select(di => di.Name))));
            return null;
        }

        DirectoryInfo GetPlatform(DirectoryInfo websiteToDeploy)
        {
            return websiteToDeploy.GetDirectories().Single();
        }

    }
}