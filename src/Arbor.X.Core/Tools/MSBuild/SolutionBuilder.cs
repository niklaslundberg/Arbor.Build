using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Exceptions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using FubuCsProjFile;
using Microsoft.Web.XmlTransform;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

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

        readonly PathLookupSpecification _pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;

        readonly List<string> _buildConfigurations = new List<string>();

        readonly List<string> _knownPlatforms = new List<string> {"x86", "x64", "Any CPU"};
        readonly List<string> _platforms = new List<string>();
        bool _appDataJobsEnabled;
        bool _pdbArtifactsEnabled;

        string _artifactsPath;
        CancellationToken _cancellationToken;
        string _msBuildExe;
        int _processorCount;
        bool _showSummary;
        MSBuildVerbositoyLevel _verbosity;
        bool _createWebDeployPackages;
        private string _vcsRoot;
        bool _configurationTransformsEnabled;
        string _defaultTarget;
        ILogger _logger;

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _logger = logger;
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

            _createWebDeployPackages = buildVariables.GetBooleanByKey(WellKnownVariables.WebDeployBuildPackages,
                defaultValue: true);

            logger.WriteVerbose(string.Format("Using MSBuild verbosity {0}", _verbosity));

            _vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            _configurationTransformsEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.GenericXmlTransformsEnabled, defaultValue:false);
            _defaultTarget = buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_DefaultTarget, "rebuild");
            _pdbArtifactsEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.PublishPdbFilesAsArtifacts, defaultValue: false);

            if (_vcsRoot == null)
            {
                logger.WriteError("Could not find version control root path");
                return ExitCode.Failure;
            }

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
            string buildConfiguration =
                variables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_BuildConfiguration,
                    defaultValue: "");

            if (!string.IsNullOrWhiteSpace(buildConfiguration))
            {
                _buildConfigurations.Add(buildConfiguration);
            }
            else
            {
                bool buildRelease = BuildPlatformOrConfiguration(variables, WellKnownVariables.ReleaseBuildEnabled);

                if (buildRelease)
                {
                    _buildConfigurations.Add("release");
                }
                else
                {
                    logger.Write(string.Format("Flag {0} is set to false, ignoring release builds",
                        WellKnownVariables.ReleaseBuildEnabled));
                }

                bool buildDebug = BuildPlatformOrConfiguration(variables, WellKnownVariables.DebugBuildEnabled);

                if (buildDebug)
                {
                    _buildConfigurations.Add("debug");
                }
                else
                {
                    logger.Write(string.Format("Flag {0} is set to false, ignoring debug builds", WellKnownVariables.DebugBuildEnabled));
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

            logger.WriteDebug("Starting finding solution files");

            Stopwatch findSolutionFiles = Stopwatch.StartNew();

            IReadOnlyCollection<FileInfo> solutionFiles = FindSolutionFiles(new DirectoryInfo(_vcsRoot), logger).ToReadOnlyCollection();

            findSolutionFiles.Stop();

            logger.WriteDebug(string.Format("Finding solutions files took {0} seconds", findSolutionFiles.Elapsed.TotalSeconds.ToString("F")));

            if (!solutionFiles.Any())
            {
                StringBuilder messageBuilder = new StringBuilder();

                messageBuilder.Append("Could not find any solution files.");

                var sourceRootDirectories = new DirectoryInfo(_vcsRoot);

                var files = sourceRootDirectories.GetFiles().Select(file => file.Name);
                var directories = sourceRootDirectories.GetDirectories().Select(dir => dir.Name);

                var all = files.Concat(directories);
                messageBuilder.Append(". Root directory files and directories");
                messageBuilder.AppendLine();

                foreach (var item in all)
                {
                    messageBuilder.AppendLine(item);
                }

                var message = messageBuilder.ToString();

                logger.WriteWarning(message);

                return ExitCode.Success;
            }

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
            bool enabled =
                variables.GetBooleanByKey(key, defaultValue: true);

            return enabled;
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
                Environment.SetEnvironmentVariable(WellKnownVariables.CurrentBuildConfiguration, configuration);
                ExitCode result =
                    await BuildSolutionWithConfigurationAsync(solutionFile, configuration, logger, platforms);

                if (!result.IsSuccess)
                {
                    return result;
                }
                Environment.SetEnvironmentVariable(WellKnownVariables.CurrentBuildConfiguration, "");
            }

            return ExitCode.Success;
        }

        async Task<ExitCode> BuildSolutionWithConfigurationAsync(FileInfo solutionFile, string configuration,
            ILogger logger, IEnumerable<string> platforms)
        {
            foreach (string knownPlatform in platforms)
            {
                Stopwatch buildStopwatch = Stopwatch.StartNew();

                logger.WriteDebug(string.Format("Starting stopwatch for solution file {0} ({1}|{2})", solutionFile.Name, configuration, knownPlatform));

                ExitCode result =
                    await BuildSolutionWithConfigurationAndPlatformAsync(solutionFile, configuration, knownPlatform,
                        logger);

                buildStopwatch.Stop();

                logger.WriteDebug(string.Format("Stopping stopwatch for solution file {0} ({1}|{2}), total time in seconds {3} ({4})", solutionFile.Name, configuration, knownPlatform, buildStopwatch.Elapsed.TotalSeconds.ToString("F"), result.IsSuccess ? "success" : "failed"));

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
                              string.Format("/target:{0}", _defaultTarget),
                              string.Format("/maxcpucount:{0}", _processorCount.ToString(CultureInfo.InvariantCulture)),
                              "/nodeReuse:false"
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

            if (exitCode.IsSuccess)
            {
                exitCode = await PublishPdbFilesAynsc(configuration, platform);
            }
            else
            {
                logger.WriteError("Skipping PDB publising since web site build failed");
            }

            return exitCode;
        }

        Task<ExitCode> PublishPdbFilesAynsc(string configuration, string platform)
        {
            _logger.Write(_pdbArtifactsEnabled
                ? $"Publishing PDB artificats for configuration {configuration} and platform {platform}"
                : $"Skipping PDF artifact publising for configuration {configuration} and platform {platform} because PDB artifact publishing is disabled");

            try
            {
                var defaultPathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;
                var ignoredDirectorySegments =
                    defaultPathLookupSpecification.IgnoredDirectorySegments.Except(new[] {"bin"});

                var pathLookupSpecification = new PathLookupSpecification(ignoredDirectorySegments,
                    defaultPathLookupSpecification.IgnoredFileStartsWithPatterns,
                    defaultPathLookupSpecification.IgnoredDirectorySegmentParts,
                    defaultPathLookupSpecification.IgnoredDirectoryStartsWithPatterns);

                var sourceRootDirectory = new DirectoryInfo(_vcsRoot);

                IReadOnlyCollection<FileInfo> files = sourceRootDirectory.GetFilesRecursive(new[] {".pdb", ".dll"},
                    pathLookupSpecification, _vcsRoot).OrderBy(file => file.FullName).ToReadOnlyCollection();

                var pdbFiles =
                    files.Where(file => file.Extension.Equals(".pdb", StringComparison.InvariantCultureIgnoreCase))
                        .ToReadOnlyCollection();

                var dllFiles =
                    files.Where(file => file.Extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
                        .ToReadOnlyCollection();

                _logger.WriteDebug(
                    $"Found files as PDB artifacts {string.Join(Environment.NewLine, pdbFiles.Select(file => "\tPDB: " + file.FullName))}");

                var pairs = pdbFiles
                    .Select(pdb => new
                                   {
                                       PdbFile = pdb,
                                       DllFile = dllFiles
                                           .SingleOrDefault(dll => dll.FullName
                                               .Equals(Path.Combine(pdb.Directory.FullName,
                                                   $"{Path.GetFileNameWithoutExtension(pdb.Name)}.dll"),
                                                   StringComparison.InvariantCultureIgnoreCase))
                                   })
                    .ToReadOnlyCollection();

                var targetDirectoryPath = Path.Combine(_artifactsPath, "PDB", configuration, platform);

                var targetDiretory = new DirectoryInfo(targetDirectoryPath).EnsureExists();

                foreach (var pair in pairs)
                {
                    var targetFilePath = Path.Combine(targetDiretory.FullName, pair.PdbFile.Name);

                    if (!File.Exists(targetFilePath))
                    {
                        _logger.WriteDebug($"Copying PDB file '{pair.PdbFile.FullName}' to '{targetFilePath}'");

                    pair.PdbFile.CopyTo(targetFilePath, CopyOptions.FailIfExists);
                    }
                    else
                    {
                        _logger.WriteDebug($"Target file '{targetFilePath}' alread exists, skipping file");
                    }
                    if (pair.DllFile != null)
                    {
                        var targetDllFilePath = Path.Combine(targetDiretory.FullName, pair.DllFile.Name);

                        if (!File.Exists(targetDllFilePath))
                        {
                            _logger.WriteDebug($"Copying DLL file '{pair.DllFile.FullName}' to '{targetFilePath}'");
                            pair.DllFile.CopyTo(targetDllFilePath, CopyOptions.FailIfExists);
                        }
                        else
                        {
                            _logger.WriteDebug($"Target DLL file '{targetDllFilePath}' alread exists, skipping file");
                        }
                    }
                    else
                    {
                        _logger.WriteDebug($"DLL file for PDB '{pair.PdbFile.FullName}' was not found");
                    }
                }

            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.WriteError($"Could not publish PDB artifacts. {ex}");
                return Task.FromResult(ExitCode.Failure);
            }
            return Task.FromResult(ExitCode.Success);
        }

        async Task<ExitCode> BuildWebApplicationsAsync(FileInfo solutionFile, string configuration, string platform,
            ILogger logger)
        {
            Solution solution = Solution.LoadFrom(solutionFile.FullName);

            List<SolutionProject> webProjects =
                solution.Projects.Where(
                    project => project.Project.ProjectTypes().Any(type => type == WebApplicationProjectTypeId)).ToList();

            logger.WriteDebug(string.Format("Finding WebApplications by looking at project type GUID {0}", WebApplicationProjectTypeId));

            logger.Write(string.Format("WebApplication projects to build [{0}]: {1}", webProjects.Count,
                string.Join(", ", webProjects.Select(wp => wp.Project.FileName))));

            foreach (SolutionProject solutionProject in webProjects)
            {
                var platformDirectoryPath = Path.Combine(_artifactsPath, "Websites", solutionProject.ProjectName, platform);

                var platformDirectory = new DirectoryInfo(platformDirectoryPath).EnsureExists();

                DirectoryInfo siteArtifactDirectory = platformDirectory.CreateSubdirectory(configuration);

                string platformName = platform == "Any CPU" ? "AnyCPU" : platform;

                var buildSiteArguments = new List<string>(15)
                              {
                                  solutionProject.Project.FileName,
                                  string.Format("/property:configuration={0}", configuration),
                                  string.Format("/property:platform={0}", platformName),
                                  string.Format("/property:_PackageTempDir={0}", siteArtifactDirectory.FullName),
// ReSharper disable once PossibleNullReferenceException
                                  string.Format("/property:SolutionDir={0}", solutionFile.Directory.FullName),
                                  string.Format("/verbosity:{0}", _verbosity.Level),
                                  "/target:pipelinePreDeployCopyAllFilesToOneFolder",
                                  "/property:AutoParameterizationWebConfigConnectionStrings=false",
                                  string.Format("/maxcpucount:{0}",
                                      _processorCount.ToString(CultureInfo.InvariantCulture)),

                                  "/nodeReuse:false"
                              };

                if (_showSummary)
                {
                    buildSiteArguments.Add("/detailedsummary");
                }

                ExitCode buildSiteExitCode =
                    await ProcessRunner.ExecuteAsync(_msBuildExe, arguments: buildSiteArguments, standardOutLog: logger.Write,
                        standardErrorAction: logger.WriteError, toolAction: logger.Write,
                        cancellationToken: _cancellationToken);

                if (!buildSiteExitCode.IsSuccess)
                {
                    return buildSiteExitCode;
                }

                if (_configurationTransformsEnabled)
                {
                    TransformFiles(configuration, logger, solutionProject, siteArtifactDirectory);
                }
                else
                {
                    logger.WriteDebug("Transforms are disabled");
                }

                if (_createWebDeployPackages)
                {
                    ExitCode packageSiteExitCode = await CreateWebDeployPackagesAsync(solutionFile, configuration, logger, platformDirectoryPath, solutionProject, platformName);

                    if (!packageSiteExitCode.IsSuccess)
                    {
                        return packageSiteExitCode;
                    }
                }

                if (_appDataJobsEnabled)
                {
                    ExitCode exitCode = await CopyKuduWebJobsAsync(logger, solutionProject, siteArtifactDirectory);

                    if (!exitCode.IsSuccess)
                    {
                        return exitCode;
                    }
                }
                else
                {
                    logger.Write("AppData Web Jobs are disabled");
                }
            }

            return ExitCode.Success;
        }

        void TransformFiles(string configuration, ILogger logger, SolutionProject solutionProject,
            DirectoryInfo siteArtifactDirectory)
        {
            logger.WriteDebug("Transforms are enabled");

            logger.WriteDebug("Starting xml transformations");

            Stopwatch transformationStopwatch = Stopwatch.StartNew();
            string projectDirectoryPath = solutionProject.Project.ProjectDirectory;

            string[] extensions = {".xml", ".config"};

            IReadOnlyCollection<FileInfo> files = new DirectoryInfo(projectDirectoryPath)
                .GetFilesRecursive(extensions)
                .Where(
                    file =>
                        !_pathLookupSpecification.IsBlackListed(file.DirectoryName) &&
                        !_pathLookupSpecification.IsFileBlackListed(file.FullName, _vcsRoot))
                .Where(
                    file =>
                        extensions.Any(
                            extension =>
                                Path.GetExtension(file.Name).Equals(extension, StringComparison.InvariantCultureIgnoreCase)))
                .Where(file => !file.Name.Equals("web.config", StringComparison.InvariantCultureIgnoreCase))
                .ToReadOnlyCollection();

            Func<FileInfo, string> transformFile = file =>
            {
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
                string extension = Path.GetExtension(file.Name);

                // ReSharper disable once PossibleNullReferenceException
                var transformFilePath = Path.Combine(file.Directory.FullName,
                    nameWithoutExtension + "." + configuration + extension);

                return transformFilePath;
            };

            var transformationPairs = files
                .Select(file => new
                                {
                                    Original = file,
                                    TransformFile = transformFile(file)
                                })
                .Where(filePair => File.Exists(filePair.TransformFile))
                .ToReadOnlyCollection();

            logger.WriteDebug(string.Format("Found {0} files with transforms", transformationPairs.Count));

            foreach (var configurationFile in transformationPairs)
            {
                string relativeFilePath = configurationFile.Original.FullName.Replace(projectDirectoryPath, "");

                string targetTransformResultPath = string.Format("{0}{1}", siteArtifactDirectory.FullName, relativeFilePath);

                var transformable = new XmlTransformableDocument();

                transformable.Load(configurationFile.Original.FullName);

                var transformation = new XmlTransformation(configurationFile.TransformFile);

                logger.WriteDebug(string.Format("Transforming '{0}' with transformation file '{1} to target file '{2}'",
                    configurationFile.Original.FullName, configurationFile.TransformFile, targetTransformResultPath));

                if (transformation.Apply(transformable))
                {
                    transformable.Save(targetTransformResultPath);
                }
            }

            transformationStopwatch.Stop();

            logger.WriteDebug(string.Format("XML transformations took {0} seconds",
                transformationStopwatch.Elapsed.TotalSeconds.ToString("F")));
        }

        async Task<ExitCode> CopyKuduWebJobsAsync(ILogger logger, SolutionProject solutionProject, DirectoryInfo siteArtifactDirectory)
        {
            logger.Write("AppData Web Jobs are enabled");
            logger.WriteDebug("Starting web deploy packaging");

            Stopwatch webJobStopwatch = Stopwatch.StartNew();

            ExitCode exitCode;

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

                    exitCode =
                        await
                            DirectoryCopy.CopyAsync(kuduWebJobs.FullName, artifactJobAppDataDirectory.FullName, logger,
                                rootDir: _vcsRoot);
                }
                else
                {
                    logger.WriteVerbose(string.Format(
                        "Site has no jobs directory in App_Data directory: '{0}'", appDataDirectory.FullName));
                    exitCode = ExitCode.Success;
                }
            }
            else
            {
                logger.WriteVerbose(string.Format("Site has no App_Data directory: '{0}'",
                    appDataDirectory.FullName));
                exitCode = ExitCode.Success;
            }

            webJobStopwatch.Stop();

            logger.WriteDebug(string.Format("Web jobs took {0} seconds", webJobStopwatch.Elapsed.TotalSeconds.ToString("F")));

            return exitCode;
        }

        async Task<ExitCode> CreateWebDeployPackagesAsync(FileInfo solutionFile, string configuration, ILogger logger,
            string platformDirectoryPath, SolutionProject solutionProject, string platformName)
        {
            logger.WriteDebug("Starting web deploy packaging");

            Stopwatch webDeployStopwatch = Stopwatch.StartNew();

            string webDeployPackageDirectoryPath = Path.Combine(platformDirectoryPath, "WebDeploy");

            var webDeployPackageDirectory = new DirectoryInfo(webDeployPackageDirectoryPath).EnsureExists();

            string packagePath = Path.Combine(webDeployPackageDirectory.FullName,
                string.Format("{0}_{1}.zip", solutionProject.ProjectName, configuration));

            var buildSitePackageArguments = new List<string>(15)
                                            {
                                                solutionProject.Project.FileName,
                                                string.Format("/property:configuration={0}", configuration),
                                                string.Format("/property:platform={0}", platformName),
// ReSharper disable once PossibleNullReferenceException
                                                string.Format("/property:SolutionDir={0}",
                                                    solutionFile.Directory.FullName),
                                                string.Format("/property:PackageLocation={0}", packagePath),
                                                string.Format("/verbosity:{0}", _verbosity.Level),
                                                "/target:Package",
                                                string.Format("/maxcpucount:{0}",
                                                    _processorCount.ToString(CultureInfo.InvariantCulture)),
                                                "/nodeReuse:false"
                                            };

            if (_showSummary)
            {
                buildSitePackageArguments.Add("/detailedsummary");
            }

            ExitCode packageSiteExitCode =
                await
                    ProcessRunner.ExecuteAsync(_msBuildExe, arguments: buildSitePackageArguments,
                        standardOutLog: logger.Write,
                        standardErrorAction: logger.WriteError, toolAction: logger.Write,
                        cancellationToken: _cancellationToken);

            webDeployStopwatch.Stop();

            logger.WriteDebug(string.Format("WebDeploy packaging took {0} seconds", webDeployStopwatch.Elapsed.TotalSeconds.ToString("F")));

            return packageSiteExitCode;
        }


        IEnumerable<FileInfo> FindSolutionFiles(DirectoryInfo directoryInfo, ILogger logger)
        {
            if (IsBlacklisted(directoryInfo))
            {
                logger.WriteDebug(string.Format("Skipping directory '{0}' when searching for solution files because the directory is blacklisted", directoryInfo.FullName));
                return Enumerable.Empty<FileInfo>();
            }

            List<FileInfo> solutionFiles = directoryInfo.EnumerateFiles("*.sln").ToList();

            foreach (DirectoryInfo subDir in directoryInfo.EnumerateDirectories())
            {
                solutionFiles.AddRange(FindSolutionFiles(subDir, logger));
            }

            return solutionFiles;
        }

        bool IsBlacklisted(DirectoryInfo directoryInfo)
        {
            bool isBlacklistedByName = _pathLookupSpecification.IsBlackListed(directoryInfo.FullName, _vcsRoot);

            bool isBlackListedByAttributes = _blackListedByAttributes.Any(
                blackListed => directoryInfo.Attributes.HasFlag(blackListed));

            return isBlacklistedByName || isBlackListedByAttributes;
        }
    }
}