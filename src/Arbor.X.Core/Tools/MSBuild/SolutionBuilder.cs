using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using FubuCsProjFile;

namespace Arbor.X.Core.Tools.MSBuild
{
    [Priority(300)]
    public class SolutionBuilder : ITool
    {
        public readonly Guid WebApplicationProjectTypeId = Guid.Parse("349C5851-65DF-11DA-9384-00065B846F21");

        readonly List<FileAttributes> _blackListedByAttributes = new List<FileAttributes>
                                                                 {
                                                                     FileAttributes.Hidden,
                                                                     FileAttributes.System,
                                                                     FileAttributes.Offline,
                                                                     FileAttributes.Archive
                                                                 };

        readonly List<string> _blackListedByName = new List<string> {"bin", "obj", ".git", "packages", "TestResults"};
        readonly List<string> _blackListedByStartName = new List<string> {"_", "."};
        readonly List<string> _buildConfigurations = new List<string>();

        readonly List<string> _knownPlatforms = new List<string> {"x86", "Any CPU"};
        readonly List<string> _platforms = new List<string>();
        bool _appDataJobsEnabled;
        string _artifactsPath;
        CancellationToken _cancellationToken;
        string _msBuildExe;
        int _processorCount;
        bool _showSummary;
        MSBuildVerbositoyLevel _verbosity;

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _msBuildExe =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSBuild_ExePath).ThrowIfEmptyValue().Value;
            _artifactsPath =
                buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;

            _appDataJobsEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.AppDataJobsEnabled,
                defaultValue: false);

            int maxProcessorCount = ProcessorCount(buildVariables);

            int maxCpuLimit = buildVariables.GetInt32ByKey(WellKnownVariables.CpuLimit, defaultValue: maxProcessorCount,
                minValue: 1);

            logger.WriteVerbose(string.Format("Using CPU limit: {0}", maxCpuLimit));

            _processorCount = maxCpuLimit;

            _verbosity =
                MSBuildVerbositoyLevel.TryParse(
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_Verbosity,
                        "normal"));

            _showSummary = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_SummaryEnabled,
                defaultValue: false);

            logger.WriteVerbose(string.Format("Using MSBuild verbosity {0}", _verbosity));

            try
            {
                return await BuildAsync(logger, buildVariables);
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.ToString());
                return ExitCode.Failure;
            }
        }

        static int ProcessorCount(IReadOnlyCollection<IVariable> buildVariables)
        {
            int processorCount = 1;

            string key = WellKnownVariables.ExternalTools_Kudu_ProcessorCount;

            int setting = buildVariables.GetInt32ByKey(key, 1);

            if (setting > 0)
            {
                processorCount = setting;
            }

            return processorCount;
        }

        async Task<ExitCode> BuildAsync(ILogger logger, IReadOnlyCollection<IVariable> variables)
        {
            string vcsRoot = variables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            if (vcsRoot == null)
            {
                logger.WriteError("Could not find version control root path");
                return ExitCode.Failure;
            }

            string buildConfiguration =
                variables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_BuildConfiguration,
                    defaultValue: "");

            if (!string.IsNullOrWhiteSpace(buildConfiguration))
            {
                _buildConfigurations.Add(buildConfiguration);
            }
            else
            {
                bool buildRelease = BuildPlatformOrConfiguration(variables, WellKnownVariables.IgnoreRelease);

                if (buildRelease)
                {
                    _buildConfigurations.Add("release");
                }
                else
                {
                    logger.Write(string.Format("Flag {0} is set, ignoring release builds",
                        WellKnownVariables.IgnoreRelease));
                }

                bool buildDebug = BuildPlatformOrConfiguration(variables, WellKnownVariables.IgnoreDebug);

                if (buildDebug)
                {
                    _buildConfigurations.Add("debug");
                }
                else
                {
                    logger.Write(string.Format("Flag {0} is set, ignoring debug builds", WellKnownVariables.IgnoreDebug));
                }
            }

            if (!_buildConfigurations.Any())
            {
                logger.WriteError("No build configurations are defined");
                return ExitCode.Failure;
            }


            string buildPlatform =
                variables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_BuildPlatform,
                    defaultValue: "");

            if (!string.IsNullOrWhiteSpace(buildPlatform))
            {
                _platforms.Add(buildPlatform);
            }
            else
            {
                foreach (string knownPlatform in _knownPlatforms)
                {
                    _platforms.Add(knownPlatform);
                }

                bool buildAnyCpu = BuildPlatformOrConfiguration(variables, WellKnownVariables.IgnoreAnyCpu);

                if (!buildAnyCpu)
                {
                    logger.Write(string.Format("Flag {0} is set, ignoring AnyCPU builds",
                        WellKnownVariables.IgnoreAnyCpu));
                    _platforms.Remove("Any CPU");
                }
            }

            if (!_platforms.Any())
            {
                logger.WriteError("No build platforms are defined");
                return ExitCode.Failure;
            }

            IEnumerable<FileInfo> solutionFiles = FindSolutionFiles(new DirectoryInfo(vcsRoot));

            IDictionary<FileInfo, IReadOnlyList<string>> solutionPlatforms =
                new Dictionary<FileInfo, IReadOnlyList<string>>();

            foreach (FileInfo solutionFile in solutionFiles)
            {
                List<string> platforms = await GetSolutionPlatformsAsync(solutionFile);

                solutionPlatforms.Add(solutionFile, platforms);
            }

            logger.WriteVerbose(string.Format("Found solutions and platforms: {0}{1}",
                Environment.NewLine,
                string.Join(Environment.NewLine,
                    solutionPlatforms.Select(
                        item => string.Format("{0}: [{1}]", item.Key, string.Join(", ", item.Value))))));

            foreach (KeyValuePair<FileInfo, IReadOnlyList<string>> solutionPlatform in solutionPlatforms)
            {
                ExitCode result = await BuildSolutionAsync(solutionPlatform.Key, solutionPlatform.Value, logger);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return ExitCode.Success;
        }

        bool BuildPlatformOrConfiguration(IReadOnlyCollection<IVariable> variables, string key)
        {
            bool ignoreVariable =
                variables.GetBooleanByKey(key, defaultValue: true);

            return ignoreVariable;
        }

        async Task<List<string>> GetSolutionPlatformsAsync(FileInfo solutionFile)
        {
            var platforms = new List<string>();

            using (var fs = new FileStream(solutionFile.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var streamReader = new StreamReader(fs))
                {
                    bool isInGlobalSection = false;

                    while (streamReader.Peek() >= 0)
                    {
                        string line = await streamReader.ReadLineAsync();

                        if (line.IndexOf("GlobalSection(SolutionConfigurationPlatforms)",
                            StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            isInGlobalSection = true;
                            continue;
                        }

                        if (line.IndexOf("EndGlobalSection",
                            StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            isInGlobalSection = false;
                            continue;
                        }

                        if (isInGlobalSection)
                        {
                            platforms.AddRange(_platforms.Where(knownPlatform =>
                                line.IndexOf(knownPlatform, StringComparison.InvariantCulture) >= 0));
                        }
                    }
                }
            }

            return platforms.Distinct().ToList();
        }

        async Task<ExitCode> BuildSolutionAsync(FileInfo solutionFile, IReadOnlyList<string> platforms, ILogger logger)
        {
            var combinations = platforms
                .SelectMany(item => _buildConfigurations.Select(config => new {Platform = item, Configuration = config}))
                .ToList();

            if (combinations.Count() > 1)
            {
                IEnumerable<Dictionary<string, string>> dictionaries =
                    combinations.Select(combination => new Dictionary<string, string>
                                                       {
                                                           {"Configuration", combination.Configuration},
                                                           {"Platform", combination.Platform}
                                                       });

                logger.WriteVerbose(string.Format("{0}{0}Configuration/platforms combinations to build: {0}{0}{1}",
                    Environment.NewLine, dictionaries.DisplayAsTable()));
            }

            foreach (string configuration in _buildConfigurations)
            {
                ExitCode result =
                    await BuildSolutionWithConfigurationAsync(solutionFile, configuration, logger, platforms);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return ExitCode.Success;
        }

        async Task<ExitCode> BuildSolutionWithConfigurationAsync(FileInfo solutionFile, string configuration,
            ILogger logger, IEnumerable<string> platforms)
        {
            foreach (string knownPlatform in platforms)
            {
                ExitCode result =
                    await BuildSolutionWithConfigurationAndPlatformAsync(solutionFile, configuration, knownPlatform,
                        logger);

                if (!result.IsSuccess)
                {
                    logger.WriteError(
                        string.Format("Could not build solution file {0} with configuration {1} and platform {2}",
                            solutionFile.FullName, configuration, knownPlatform));
                    return result;
                }
            }

            return ExitCode.Success;
        }

        async Task<ExitCode> BuildSolutionWithConfigurationAndPlatformAsync(FileInfo solutionFile, string configuration,
            string platform,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(_msBuildExe))
            {
                logger.WriteError("MSBuild path is not defined");
                return ExitCode.Failure;
            }

            if (!File.Exists(_msBuildExe))
            {
                logger.WriteError(string.Format("The MSBuild path '{0}' does not exist", _msBuildExe));
                return ExitCode.Failure;
            }

            var argList = new List<string>
                          {
                              solutionFile.FullName,
                              string.Format("/property:configuration={0}", configuration),
                              string.Format("/property:platform={0}", platform),
                              string.Format("/verbosity:{0}", _verbosity.Level),
                              "/target:rebuild",
                              string.Format("/maxcpucount:{0}", _processorCount.ToString(CultureInfo.InvariantCulture))
                          };

            if (_showSummary)
            {
                argList.Add("/detailedsummary");
            }

            logger.Write(string.Format("Building solution file {0} ({1}|{2})", solutionFile.Name, configuration,
                platform));
            logger.WriteVerbose(string.Format("{0}MSBuild arguments: {0}{0}{1}", Environment.NewLine,
                argList.Select(arg => new Dictionary<string, string> {{"Value", arg}}).DisplayAsTable()));

            ExitCode exitCode =
                await ProcessRunner.ExecuteAsync(_msBuildExe, arguments: argList, standardOutLog: logger.Write,
                    standardErrorAction: logger.WriteError, toolAction: logger.Write,
                    cancellationToken: _cancellationToken, verboseAction: logger.WriteVerbose);

            if (exitCode.IsSuccess)
            {
                ExitCode webAppsExiteCode =
                    await BuildWebApplicationsAsync(solutionFile, configuration, platform, logger);

                exitCode = webAppsExiteCode;
            }
            else
            {
                logger.WriteError("Skipping web site build since solution build failed");
            }

            return exitCode;
        }

        async Task<ExitCode> BuildWebApplicationsAsync(FileInfo solutionFile, string configuration, string platform,
            ILogger logger)
        {
            Solution solution = Solution.LoadFrom(solutionFile.FullName);

            List<SolutionProject> webProjects =
                solution.Projects.Where(
                    project => project.Project.ProjectTypes().Any(type => type == WebApplicationProjectTypeId)).ToList();

            logger.Write(string.Format("WebApplication projects to build [{0}]: {1}", webProjects.Count,
                string.Join(", ", webProjects.Select(wp => wp.Project.FileName))));

            foreach (SolutionProject solutionProject in webProjects)
            {
                DirectoryInfo siteArtifactDirectory =
                    new DirectoryInfo(Path.Combine(_artifactsPath, "Websites", solutionProject.ProjectName, platform,
                        configuration)).EnsureExists();

                string platformName = platform == "Any CPU" ? "AnyCPU" : platform;

                var argList = new List<string>
                              {
                                  solutionProject.Project.FileName,
                                  string.Format("/property:configuration={0}", configuration),
                                  string.Format("/property:platform={0}", platformName),
                                  string.Format("/property:_PackageTempDir={0}", siteArtifactDirectory),
// ReSharper disable once PossibleNullReferenceException
                                  string.Format("/property:SolutionDir={0}", solutionFile.Directory.FullName),
                                  string.Format("/verbosity:{0}", _verbosity.Level),
                                  "/target:pipelinePreDeployCopyAllFilesToOneFolder",
                                  "/property:AutoParameterizationWebConfigConnectionStrings=false",
                                  string.Format("/maxcpucount:{0}",
                                      _processorCount.ToString(CultureInfo.InvariantCulture))
                              };

                if (_showSummary)
                {
                    argList.Add("/detailedsummary");
                }

                ExitCode exitCode =
                    await ProcessRunner.ExecuteAsync(_msBuildExe, arguments: argList, standardOutLog: logger.Write,
                        standardErrorAction: logger.WriteError, toolAction: logger.Write,
                        cancellationToken: _cancellationToken);

                if (!exitCode.IsSuccess)
                {
                    return exitCode;
                }

                if (_appDataJobsEnabled)
                {
                    logger.Write("AppData Web Jobs are enabled");

                    string appDataPath = Path.Combine(solutionProject.Project.ProjectDirectory, "App_Data");

                    var appDataDirectory = new DirectoryInfo(appDataPath);

                    if (appDataDirectory.Exists)
                    {
                        logger.WriteVerbose(string.Format("Site has App_Data directory: '{0}'",
                            appDataDirectory.FullName));

                        DirectoryInfo kuduWebJobs =
                            appDataDirectory.EnumerateDirectories()
                                .SingleOrDefault(
                                    directory =>
                                        directory.Name.Equals("jobs", StringComparison.InvariantCultureIgnoreCase));

                        if (kuduWebJobs != null && kuduWebJobs.Exists)
                        {
                            logger.WriteVerbose(string.Format("Site has App_Data jobs directory: '{0}'",
                                kuduWebJobs.FullName));
                            string artifactJobAppDataPath = Path.Combine(siteArtifactDirectory.FullName, "App_Data",
                                "jobs");

                            DirectoryInfo artifactJobAppDataDirectory =
                                new DirectoryInfo(artifactJobAppDataPath).EnsureExists();

                            logger.WriteVerbose(string.Format("Copying directory '{0}' to '{1}'", kuduWebJobs.FullName,
                                artifactJobAppDataDirectory.FullName));

                            DirectoryCopy.Copy(kuduWebJobs.FullName, artifactJobAppDataDirectory.FullName);
                        }
                        else
                        {
                            logger.WriteVerbose(string.Format(
                                "Site has no jobs directory in App_Data directory: '{0}'", appDataDirectory.FullName));
                        }
                    }
                    else
                    {
                        logger.WriteVerbose(string.Format("Site has no App_Data directory: '{0}'",
                            appDataDirectory.FullName));
                    }
                }
                else
                {
                    logger.Write("AppData Web Jobs are disabled");
                }
            }

            return ExitCode.Success;
        }


        IEnumerable<FileInfo> FindSolutionFiles(DirectoryInfo directoryInfo)
        {
            if (IsBlacklisted(directoryInfo))
            {
                return Enumerable.Empty<FileInfo>();
            }

            List<FileInfo> solutionFiles = directoryInfo.EnumerateFiles("*.sln").ToList();

            foreach (DirectoryInfo subDir in directoryInfo.EnumerateDirectories())
            {
                solutionFiles.AddRange(FindSolutionFiles(subDir));
            }

            return solutionFiles;
        }

        bool IsBlacklisted(DirectoryInfo directoryInfo)
        {
            bool isBlacklistedByFullName = _blackListedByName.Any(
                blackListed => directoryInfo.Name.Equals(blackListed, StringComparison.InvariantCultureIgnoreCase));

            bool blackListedByStartName = _blackListedByStartName.Any(
                blackListed => directoryInfo.Name.StartsWith(blackListed, StringComparison.InvariantCultureIgnoreCase));

            bool isBlackListedByAttributes = _blackListedByAttributes.Any(
                blackListed => directoryInfo.Attributes.HasFlag(blackListed));

            return isBlacklistedByFullName || blackListedByStartName || isBlackListedByAttributes;
        }
    }
}