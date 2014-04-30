using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            _kuduEnabled = buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_Enabled) && bool.Parse(buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Enabled).Value);
            if (!_kuduEnabled)
            {
                return ExitCode.Success;
            }
            _artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;
            _platform = buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_Platform).ThrowIfEmptyValue().Value;
            _deployBranch = new BranchName(buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_DeploymentBranchName).Value);
            _deploymentTargetDirectory =
                buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_DeploymentTarget).Value;

            _kuduConfigurationFallback = buildVariables.HasKey(WellKnownVariables.KuduConfigurationFallback)
                ? buildVariables.Require(WellKnownVariables.KuduConfigurationFallback).Value
                : "";

            var branchNameOverride = buildVariables.SingleOrDefault(bv => bv.Key.Equals(WellKnownVariables.ExternalTools_Kudu_DeploymentBranchNameOverride, StringComparison.InvariantCultureIgnoreCase));

            if (branchNameOverride != null)
            {
                logger.Write(string.Format("Using branch name override '{0}' instead of branch name '{1}'", branchNameOverride.Value, _deployBranch));
                _deployBranch = new BranchName(branchNameOverride.Value);
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

            logger.Write(string.Format("___________________ Kudu deploy ___________________ \r\nDeploying website {0}, platform {1}, configuration {2}", websiteToDeploy.Name, platform.Name, configuration.Name));

            logger.Write(string.Format("Copying files and directories from '{0}' to '{1}'", configuration.FullName, _deploymentTargetDirectory));
            try
            {
                DirectoryCopy.Copy(configuration.FullName, _deploymentTargetDirectory);
            }
            catch (Exception ex)
            {
                logger.WriteError("Kudu deploy could not copy files " + ex.ToString());
                return ExitCode.Failure;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken); //TODO temp to avoid compiler warning

            return ExitCode.Success;
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