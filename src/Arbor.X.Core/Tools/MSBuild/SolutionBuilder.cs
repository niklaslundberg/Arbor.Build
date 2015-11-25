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
using Arbor.X.Core.Tools.NuGet;

using FubuCsProjFile;
using FubuCsProjFile.MSBuild;

using JetBrains.Annotations;
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

        bool _createNuGetWebPackage;

        IReadOnlyCollection<IVariable> _buildVariables;

        string _ruleset;

        bool _codeAnalysisEnabled;

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _buildVariables = buildVariables;
            _logger = logger;
            _cancellationToken = cancellationToken;
            _msBuildExe =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSBuild_ExePath).ThrowIfEmptyValue().Value;
            _artifactsPath =
                buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;

            _appDataJobsEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.AppDataJobsEnabled,
                defaultValue: false);

            _codeAnalysisEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.ExternalTools_MSBuild_CodeAnalysisEnabled,
                    defaultValue: false);

            int maxProcessorCount = ProcessorCount(buildVariables);

            int maxCpuLimit = buildVariables.GetInt32ByKey(WellKnownVariables.CpuLimit, defaultValue: maxProcessorCount,
                minValue: 1);

            logger.WriteVerbose($"Using CPU limit: {maxCpuLimit}");

            _processorCount = maxCpuLimit;

            _verbosity =
                MSBuildVerbositoyLevel.TryParse(
                    buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_Verbosity,
                        "normal"));

            _showSummary = buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_SummaryEnabled,
                defaultValue: false);

            _createWebDeployPackages = buildVariables.GetBooleanByKey(WellKnownVariables.WebDeployBuildPackages,
                defaultValue: true);

            logger.WriteVerbose($"Using MSBuild verbosity {_verbosity}");

            _vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            if (_codeAnalysisEnabled)
            {
                _ruleset = FindRuleSet();
            }
            else
            {
                _logger.WriteVerbose("Code analysis is disabled, skipping ruleset lookup.");
            }

            _configurationTransformsEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.GenericXmlTransformsEnabled, defaultValue:false);
            _defaultTarget = buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_MSBuild_DefaultTarget, "rebuild");
            _pdbArtifactsEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.PublishPdbFilesAsArtifacts, defaultValue: false);
            _createNuGetWebPackage = buildVariables.GetBooleanByKey(WellKnownVariables.NugetCreateNuGetWebPackagesEnabled, defaultValue: false);

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

        string FindRuleSet()
        {
            var fileInfos = new DirectoryInfo(_vcsRoot)
                .GetFiles("*.ruleset", SearchOption.AllDirectories)
                .Where(file => _pathLookupSpecification.IsFileBlackListed(file.FullName, _vcsRoot)).ToReadOnlyCollection();

            if (fileInfos.Count != 1)
            {
                if (fileInfos.Count == 0)
                {
                    _logger.WriteVerbose("Could not find any ruleset file (.ruleset) in solution");
                }
                else
                {
                    _logger.WriteVerbose(
                        $"Found {fileInfos.Count} ruleset files (.ruleset) in solution, only one is supported, skipping code analysis with rules");
                }

                return null;
            }

            string foundRuleSet = fileInfos.Single().FullName;

            _logger.WriteVerbose($"Found one ruleset file '{foundRuleSet}'");

            return foundRuleSet;
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
                bool buildDebug = BuildPlatformOrConfiguration(variables, WellKnownVariables.DebugBuildEnabled);

                if (buildDebug)
                {
                    _buildConfigurations.Add("debug");
                }
                else
                {
                    logger.Write($"Flag {WellKnownVariables.DebugBuildEnabled} is set to false, ignoring debug builds");
                }

                bool buildRelease = BuildPlatformOrConfiguration(variables, WellKnownVariables.ReleaseBuildEnabled);

                if (buildRelease)
                {
                    _buildConfigurations.Add("release");
                }
                else
                {
                    logger.Write(
                        $"Flag {WellKnownVariables.ReleaseBuildEnabled} is set to false, ignoring release builds");
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
                    logger.Write($"Flag {WellKnownVariables.IgnoreAnyCpu} is set, ignoring AnyCPU builds");
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

            logger.WriteDebug(
                $"Finding solutions files took {findSolutionFiles.Elapsed.TotalSeconds.ToString("F")} seconds");

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

            logger.WriteVerbose(
                $"Found solutions and platforms: {Environment.NewLine}{string.Join(Environment.NewLine, solutionPlatforms.Select(item => $"{item.Key}: [{string.Join(", ", item.Value)}]"))}");

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

                logger.WriteDebug(
                    $"Starting stopwatch for solution file {solutionFile.Name} ({configuration}|{knownPlatform})");

                ExitCode result =
                    await BuildSolutionWithConfigurationAndPlatformAsync(solutionFile, configuration, knownPlatform,
                        logger);

                buildStopwatch.Stop();

                logger.WriteDebug(
                    $"Stopping stopwatch for solution file {solutionFile.Name} ({configuration}|{knownPlatform}), total time in seconds {buildStopwatch.Elapsed.TotalSeconds.ToString("F")} ({(result.IsSuccess ? "success" : "failed")})");

                if (!result.IsSuccess)
                {
                    logger.WriteError(
                        $"Could not build solution file {solutionFile.FullName} with configuration {configuration} and platform {knownPlatform}");
                    return result;
                }
            }

            return ExitCode.Success;
        }

        async Task<ExitCode> BuildSolutionWithConfigurationAndPlatformAsync(
            FileInfo solutionFile,
            string configuration,
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
                logger.WriteError($"The MSBuild path '{_msBuildExe}' does not exist");
                return ExitCode.Failure;
            }

            var argList = new List<string>
                              {
                                  solutionFile.FullName,
                                  $"/property:configuration={configuration}",
                                  $"/property:platform={platform}",
                                  $"/verbosity:{_verbosity.Level}",
                                  $"/target:{_defaultTarget}",
                                  $"/maxcpucount:{_processorCount.ToString(CultureInfo.InvariantCulture)}",
                                  "/nodeReuse:false"
                              };

            if (_codeAnalysisEnabled)
            {
                logger.WriteVerbose("Code analysis is enabled");

                argList.Add("/property:RunCodeAnalysis=true");

                if (!string.IsNullOrWhiteSpace(_ruleset) && File.Exists(_ruleset))
                {
                    logger.Write($"Using code analysis ruleset '{_ruleset}'");

                    argList.Add($"/property:CodeAnalysisRuleSet={_ruleset}");
                }
            }
            else
            {
                logger.Write("Code analysis is disabled");
            }

            if (_showSummary)
            {
                argList.Add("/detailedsummary");
            }

            logger.Write($"Building solution file {solutionFile.Name} ({configuration}|{platform})");
            logger.WriteVerbose(
                string.Format(
                    "{0}MSBuild arguments: {0}{0}{1}",
                    Environment.NewLine,
                    argList.Select(arg => new Dictionary<string, string> { { "Value", arg } }).DisplayAsTable()));

            ExitCode exitCode =
                await
                ProcessRunner.ExecuteAsync(
                    _msBuildExe,
                    arguments: argList,
                    standardOutLog: logger.Write,
                    standardErrorAction: logger.WriteError,
                    toolAction: logger.Write,
                    cancellationToken: _cancellationToken,
                    verboseAction: logger.WriteVerbose);

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

            var analysisLogFiles =
                new DirectoryInfo(_vcsRoot).GetFiles("*.AnalysisLog.xml", SearchOption.AllDirectories)
                    .ToReadOnlyCollection();

            var targetReportDirectory =
                new DirectoryInfo(Path.Combine(_artifactsPath, "CodeAnalysis")).EnsureExists();

            logger.WriteVerbose(
                $"Found {analysisLogFiles.Count} code analysis log files: {string.Join(Environment.NewLine, analysisLogFiles.Select(file => file.FullName))}");

            foreach (var analysisLogFile in analysisLogFiles)
            {
                var projectName= analysisLogFile.Name.Replace(".CodeAnalysisLog.xml", "");

                var targetFilePath = Path.Combine(
                    targetReportDirectory.FullName,
                    $"{projectName}.{Platforms.Normalize(platform)}.{configuration}.xml");

                analysisLogFile.CopyTo(targetFilePath, CopyOptions.None);
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

                var targetDirectoryPath = Path.Combine(_artifactsPath, "PDB", configuration, Platforms.Normalize(platform));

                var targetDirectory = new DirectoryInfo(targetDirectoryPath).EnsureExists();

                foreach (var pair in pairs)
                {
                    var targetFilePath = Path.Combine(targetDirectory.FullName, pair.PdbFile.Name);

                    if (!File.Exists(targetFilePath))
                    {
                        _logger.WriteDebug($"Copying PDB file '{pair.PdbFile.FullName}' to '{targetFilePath}'");

                    pair.PdbFile.CopyTo(targetFilePath, CopyOptions.FailIfExists);
                    }
                    else
                    {
                        _logger.WriteDebug($"Target file '{targetFilePath}' already exists, skipping file");
                    }
                    if (pair.DllFile != null)
                    {
                        var targetDllFilePath = Path.Combine(targetDirectory.FullName, pair.DllFile.Name);

                        if (!File.Exists(targetDllFilePath))
                        {
                            _logger.WriteDebug($"Copying DLL file '{pair.DllFile.FullName}' to '{targetFilePath}'");
                            pair.DllFile.CopyTo(targetDllFilePath, CopyOptions.FailIfExists);
                        }
                        else
                        {
                            _logger.WriteDebug($"Target DLL file '{targetDllFilePath}' already exists, skipping file");
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

            logger.WriteDebug($"Finding WebApplications by looking at project type GUID {WebApplicationProjectTypeId}");

            logger.Write(
                $"WebApplication projects to build [{webProjects.Count}]: {string.Join(", ", webProjects.Select(wp => wp.Project.FileName))}");

            foreach (SolutionProject solutionProject in webProjects)
            {
                var platformDirectoryPath = Path.Combine(_artifactsPath, "Websites", solutionProject.ProjectName, Platforms.Normalize(platform));

                var platformDirectory = new DirectoryInfo(platformDirectoryPath).EnsureExists();

                DirectoryInfo siteArtifactDirectory = platformDirectory.CreateSubdirectory(configuration);

                string platformName = Platforms.Normalize(platform);

                var buildSiteArguments = new List<string>(15)
                              {
                                  solutionProject.Project.FileName,
                                  $"/property:configuration={configuration}",
                                  $"/property:platform={platformName}",
                                  $"/property:_PackageTempDir={siteArtifactDirectory.FullName}",
// ReSharper disable once PossibleNullReferenceException
                                  $"/property:SolutionDir={solutionFile.Directory.FullName}",
                                  $"/verbosity:{_verbosity.Level}",
                                  "/target:pipelinePreDeployCopyAllFilesToOneFolder",
                                  "/property:AutoParameterizationWebConfigConnectionStrings=false",
                                  $"/maxcpucount:{_processorCount.ToString(CultureInfo.InvariantCulture)}",

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
                    logger.Write(
                        $"Web Deploy package creation is enabled, creating package for {solutionProject.ProjectName}");

                    ExitCode packageSiteExitCode =
                        await
                        CreateWebDeployPackagesAsync(
                            solutionFile,
                            configuration,
                            logger,
                            platformDirectoryPath,
                            solutionProject,
                            platformName);

                    if (!packageSiteExitCode.IsSuccess)
                    {
                        return packageSiteExitCode;
                    }
                }
                else
                {
                    logger.Write("Web Deploy package creation is disabled");
                }

                if (_createNuGetWebPackage)
                {
                    logger.Write(
                        $"NuGet web package creation is enabled, creating NuGet package for {solutionProject.ProjectName}");

                    ExitCode packageSiteExitCode =
                        await
                        CreateNuGetWebPackagesAsync(
                            solutionFile,
                            configuration,
                            logger,
                            platformDirectoryPath,
                            solutionProject,
                            platformName, siteArtifactDirectory.FullName);

                    if (!packageSiteExitCode.IsSuccess)
                    {
                        return packageSiteExitCode;
                    }
                }
                else
                {
                    logger.Write(
                        $"NuGet web package creation is disabled, build variable '{WellKnownVariables.NugetCreateNuGetWebPackagesEnabled}' is not set or value is other than true");
                }

                if (_appDataJobsEnabled)
                {
                    logger.Write("AppData Web Jobs are enabled");

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

        async Task<ExitCode> CreateNuGetWebPackagesAsync(FileInfo solutionFile, string configuration, ILogger logger, string platformDirectoryPath, SolutionProject solutionProject, string platformName, string siteArtifactDirectory)
        {
            if (!platformName.Equals(Platforms.Normalize(WellKnownPlatforms.AnyCPU), StringComparison.InvariantCultureIgnoreCase))
            {
                logger.WriteWarning(
                    $"Only '{WellKnownPlatforms.AnyCPU}' platform is supported for NuGet web packages, skipping platform '{platformName}'");
                return ExitCode.Success;
            }

            string expectedName = string.Format(WellKnownVariables.NugetCreateNuGetWebPackageForProjectEnabledFormat, solutionProject.ProjectName.Replace(".", "_").Replace(" ", "_").Replace("-","_"));

            List<MSBuildProperty> msbuildProperties = solutionProject.Project.BuildProject.PropertyGroups.SelectMany(s => s.Properties)
                .Where(msBuildProperty => msBuildProperty.Name.Equals(expectedName, StringComparison.InvariantCultureIgnoreCase)).ToList();


            bool buildNuGetWebPackageForProject = true;

            if (msbuildProperties.Any())
            {
                List<ParseResult<bool>> parseResults = msbuildProperties.Select(
                    msBuildProperty =>
                        {
                            ParseResult<bool> parseResult = msBuildProperty.Value.TryParseBool(defaultValue: true);
                            return parseResult;
                        }).Where(item => item.Parsed).ToList();


                if (parseResults.Any())
                {
                    bool hasAnyPropertySetToFalse = parseResults.Any(item => !item.Value);

                    if (hasAnyPropertySetToFalse)
                    {
                        _logger.WriteVerbose(
                            $"Build NuGet web package is disabled in project file '{solutionProject.Project.FileName}'; property '{expectedName}'");
                        buildNuGetWebPackageForProject = false;
                    }
                    else
                    {
                        _logger.WriteVerbose(
                            $"Build NuGet web package is enabled via project file '{solutionProject.Project.FileName}'; property '{expectedName}'");
                    }
                }
                else
                {
                    _logger.WriteDebug(
                        $"Build NuGet web package is not configured in project file '{solutionProject.Project.FileName}'; property '{expectedName}', invalid value");

                }
            }
            else
            {
                _logger.WriteDebug(
                    $"Build NuGet web package is not configured in project file '{solutionProject.Project.FileName}'; property '{expectedName}'");
            }

            string buildVariable = _buildVariables.GetVariableValueOrDefault(expectedName, defaultValue: "");

            if (!string.IsNullOrWhiteSpace(buildVariable))
            {
                ParseResult<bool> parseResult = buildVariable.TryParseBool(defaultValue: true);

                if (parseResult.Parsed && !parseResult.Value)
                {
                    _logger.WriteVerbose($"Build NuGet web package is turned off in build variable '{expectedName}'");
                    buildNuGetWebPackageForProject = false;
                }
                else if (parseResult.Parsed)
                {
                    _logger.WriteDebug($"Build NuGet web package is enabled in build variable '{expectedName}'");
                }
                else
                {
                    _logger.WriteDebug($"Build NuGet web package is not configured in build variable '{expectedName}'");
                }
            }
            else
            {
                _logger.WriteDebug($"Build NuGet web package is not configured using build variable '{expectedName}', variable is not defined");
            }

            if (!buildNuGetWebPackageForProject)
            {
                logger.Write($"Creating NuGet web package for project '{solutionProject.ProjectName}' is disabled");
                return ExitCode.Success;
            }

            logger.Write($"Creating NuGet web package for project '{solutionProject.ProjectName}'");

            var packageId = solutionProject.ProjectName;

            string files =
                $@"<file src=""{siteArtifactDirectory}\**\*.*"" target=""Content"" exclude=""packages.config"" />";

            ExitCode exitCode = await CreateNuGetPackageAsync(platformDirectoryPath, logger, packageId, files);

            if (!exitCode.IsSuccess)
            {
                logger.WriteError($"Failed to create NuGet web package for project '{solutionProject.ProjectName}'");
                return exitCode;
            }

            const string EnvironmentLiteral = "Environment";
            const string Pattern = "{Name}." + EnvironmentLiteral + ".{EnvironmentName}.{action}.{extension}";
            var separator = '.';
            int fileNameMinPartCount = Pattern.Split(separator).Count();

            var environmentFiles = new DirectoryInfo(solutionProject.Project.ProjectDirectory)
                .GetFilesRecursive(rootDir:_vcsRoot)
                .Select(file => new {File=file, Parts=file.Name.Split(separator)})
                .Where(item => item.Parts.Length == fileNameMinPartCount)
                .Where(item => item.Parts[1].Equals(EnvironmentLiteral, StringComparison.OrdinalIgnoreCase))
                .Select(item => new {File=item.File, EnvironmentName=item.Parts[2]})
                .SafeToReadOnlyCollection();

            IReadOnlyCollection<string> environmentNames = environmentFiles
                .Select(group => new { Key = group.EnvironmentName, InvariantKey = group.EnvironmentName.ToLowerInvariant() })
                .GroupBy(item => item.InvariantKey)
                .Select(grouping => grouping.First().Key)
                .Distinct()
                .SafeToReadOnlyCollection();

            string rootDirectory =
                solutionProject.Project.ProjectDirectory.Trim(Path.DirectorySeparatorChar);

            _logger.WriteVerbose($"Found [{environmentNames.Count}] environnent names in project '{solutionProject.ProjectName}'");

            foreach (string environmentName in environmentNames)
            {
                _logger.WriteVerbose($"Creating Environment package for project '{solutionProject.ProjectName}', environment name '{environmentName}'");
                List<string> elements = environmentFiles
                    .Select(
                        file =>
                            {
                                string sourceFullPath = file.File.FullName.Trim(Path.DirectorySeparatorChar);
                                var relativePath = sourceFullPath.Replace(rootDirectory, "").Trim(Path.DirectorySeparatorChar);
                                return new { SourceFullPath = sourceFullPath, RelativePath = relativePath };
                            })
                    .Select(environmentFile => $@"<file src=""{environmentFile.SourceFullPath}"" target=""Content\{environmentFile.RelativePath}"" />")
                    .ToList();

                _logger.WriteVerbose($"Found '{elements.Count}' environment specific files in project directory '{solutionProject.Project.ProjectDirectory}' environment name '{environmentName}'");

                string environmentPackageId = $"{packageId}.Environment.{environmentName}";

                ExitCode environmentPackageExitCode
                    = await CreateNuGetPackageAsync(platformDirectoryPath, logger, environmentPackageId, string.Join(Environment.NewLine, elements));

                if (!environmentPackageExitCode.IsSuccess)
                {
                    logger.WriteError($"Failed to create NuGet environment web package for project {solutionProject.ProjectName}");
                    return environmentPackageExitCode;
                }
            }


            logger.Write($"Successfully created NuGet web package for project {solutionProject.ProjectName}");

            return ExitCode.Success;
        }

        private async Task<ExitCode> CreateNuGetPackageAsync(string platformDirectoryPath, ILogger logger, string packageId, string filesList)
        {

            const string XmlTemplate = @"<?xml version=""1.0""?>
<package >
    <metadata>
        <id>{0}</id>
        <version>{1}</version>
        <title>{2}</title>
        <authors>{3}</authors>
        <owners>{4}</owners>
        <description>
            {5}
        </description>
        <releaseNotes>
        </releaseNotes>
        <summary>
            {6}
        </summary>
        <language>{7}</language>
        <projectUrl>{8}</projectUrl>
        <iconUrl>{9}</iconUrl>
        <requireLicenseAcceptance>{10}</requireLicenseAcceptance>
        <licenseUrl>{11}</licenseUrl>
        <copyright>{12}</copyright>
        <dependencies>

        </dependencies>
        <references></references>
        <tags>{13}</tags>
    </metadata>
    <files>
        {14}
    </files>
</package>";

            string packageDirectoryPath = Path.Combine(platformDirectoryPath, "NuGet");

            DirectoryInfo packageDirectory = new DirectoryInfo(packageDirectoryPath).EnsureExists();

            NuGetPackageConfiguration packageConfiguration = NuGetPackager.GetNuGetPackageConfiguration(
                logger,
                _buildVariables,
                packageDirectory.FullName,
                _vcsRoot);


            string name = packageId;

            string version = packageConfiguration.Version;
            string authors = _buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCompany, "Undefined");
            string owners = _buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCompany, "Undefined");
            string description = packageId;
            string summary = packageId;
            string language = "en-US";
            string projectUrl = "http://nuget.org";
            string iconUrl = "http://nuget.org";
            string requireLicenseAcceptance = "false";
            string licenseUrl = "http://nuget.org";
            string copyright = _buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCopyright, "Undefined");
            string tags = "";

            var files = filesList;

            string nuspecContent = string.Format(
                XmlTemplate,
                name,
                version,
                name,
                authors,
                owners,
                description,
                summary,
                language,
                projectUrl,
                iconUrl,
                requireLicenseAcceptance,
                licenseUrl,
                copyright,
                tags, files);


            logger.Write(nuspecContent);

            DirectoryInfo tempDir = new DirectoryInfo(Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString())).EnsureExists();

            string nuspecTempFile = Path.Combine(tempDir.FullName, $"{packageId}.nuspec");

            File.WriteAllText(nuspecTempFile, nuspecContent, Encoding.UTF8);

            ExitCode exitCode = await new NuGetPackager(_logger).CreatePackageAsync(nuspecTempFile, packageConfiguration, _cancellationToken);

            return exitCode;
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

            logger.WriteDebug($"Found {transformationPairs.Count} files with transforms");

            foreach (var configurationFile in transformationPairs)
            {
                string relativeFilePath = configurationFile.Original.FullName.Replace(projectDirectoryPath, "");

                string targetTransformResultPath = $"{siteArtifactDirectory.FullName}{relativeFilePath}";

                var transformable = new XmlTransformableDocument();

                transformable.Load(configurationFile.Original.FullName);

                var transformation = new XmlTransformation(configurationFile.TransformFile);

                logger.WriteDebug(
                    $"Transforming '{configurationFile.Original.FullName}' with transformation file '{configurationFile.TransformFile} to target file '{targetTransformResultPath}'");

                if (transformation.Apply(transformable))
                {
                    transformable.Save(targetTransformResultPath);
                }
            }

            transformationStopwatch.Stop();

            logger.WriteDebug(
                $"XML transformations took {transformationStopwatch.Elapsed.TotalSeconds.ToString("F")} seconds");
        }

        [NotNull]
        async Task<ExitCode> CopyKuduWebJobsAsync([NotNull] ILogger logger, [NotNull] SolutionProject solutionProject, [NotNull] DirectoryInfo siteArtifactDirectory)
        {
            logger.Write("AppData Web Jobs are enabled");
            logger.WriteDebug("Starting web deploy packaging");

            Stopwatch webJobStopwatch = Stopwatch.StartNew();

            ExitCode exitCode;

            string appDataPath = Path.Combine(solutionProject.Project.ProjectDirectory, "App_Data");

            var appDataDirectory = new DirectoryInfo(appDataPath);

            if (appDataDirectory.Exists)
            {
                logger.WriteVerbose($"Site has App_Data directory: '{appDataDirectory.FullName}'");

                DirectoryInfo kuduWebJobs =
                    appDataDirectory.EnumerateDirectories()
                        .SingleOrDefault(
                            directory =>
                                directory.Name.Equals("jobs", StringComparison.InvariantCultureIgnoreCase));

                if (kuduWebJobs != null && kuduWebJobs.Exists)
                {
                    logger.WriteVerbose($"Site has App_Data jobs directory: '{kuduWebJobs.FullName}'");
                    string artifactJobAppDataPath = Path.Combine(siteArtifactDirectory.FullName, "App_Data",
                        "jobs");

                    DirectoryInfo artifactJobAppDataDirectory =
                        new DirectoryInfo(artifactJobAppDataPath).EnsureExists();

                    logger.WriteVerbose(
                        $"Copying directory '{kuduWebJobs.FullName}' to '{artifactJobAppDataDirectory.FullName}'");

                    exitCode =
                        await
                            DirectoryCopy.CopyAsync(kuduWebJobs.FullName, artifactJobAppDataDirectory.FullName, logger,
                                rootDir: _vcsRoot);
                }
                else
                {
                    logger.WriteVerbose(
                        $"Site has no jobs directory in App_Data directory: '{appDataDirectory.FullName}'");
                    exitCode = ExitCode.Success;
                }
            }
            else
            {
                logger.WriteVerbose($"Site has no App_Data directory: '{appDataDirectory.FullName}'");
                exitCode = ExitCode.Success;
            }

            webJobStopwatch.Stop();

            logger.WriteDebug($"Web jobs took {webJobStopwatch.Elapsed.TotalSeconds.ToString("F")} seconds");

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
                $"{solutionProject.ProjectName}_{configuration}.zip");

            var buildSitePackageArguments = new List<string>(15)
                                            {
                                                solutionProject.Project.FileName,
                                                $"/property:configuration={configuration}",
                                                $"/property:platform={platformName}",
// ReSharper disable once PossibleNullReferenceException
                                                $"/property:SolutionDir={solutionFile.Directory.FullName}",
                                                $"/property:PackageLocation={packagePath}",
                                                $"/verbosity:{_verbosity.Level}",
                                                "/target:Package",
                                                $"/maxcpucount:{_processorCount.ToString(CultureInfo.InvariantCulture)}",
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

            logger.WriteDebug(
                $"WebDeploy packaging took {webDeployStopwatch.Elapsed.TotalSeconds.ToString("F")} seconds");

            return packageSiteExitCode;
        }


        IEnumerable<FileInfo> FindSolutionFiles(DirectoryInfo directoryInfo, ILogger logger)
        {
            if (IsBlacklisted(directoryInfo))
            {
                logger.WriteDebug(
                    $"Skipping directory '{directoryInfo.FullName}' when searching for solution files because the directory is blacklisted");
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