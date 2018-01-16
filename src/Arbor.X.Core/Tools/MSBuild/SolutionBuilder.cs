using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive.Collections;
using Arbor.Exceptions;
using Arbor.KVConfiguration.Core.Metadata;
using Arbor.KVConfiguration.Schema.Json;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Parsing;
using Arbor.X.Core.Tools.NuGet;
using FubuCore.Reflection;
using FubuCsProjFile;
using FubuCsProjFile.MSBuild;
using JetBrains.Annotations;
using Microsoft.Web.XmlTransform;

namespace Arbor.X.Core.Tools.MSBuild
{
    [Priority(300)]
    [UsedImplicitly]
    public class SolutionBuilder : ITool
    {
        readonly List<FileAttributes> _blackListedByAttributes = new List<FileAttributes>
        {
            FileAttributes.Hidden,
            FileAttributes.System,
            FileAttributes.Offline,
            FileAttributes.Archive
        };

        readonly List<string> _buildConfigurations = new List<string>();

        readonly List<string> _knownPlatforms = new List<string> { "x86", "x64", "Any CPU" };

        readonly PathLookupSpecification _pathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;
        readonly List<string> _platforms = new List<string>();

        bool _appDataJobsEnabled;

        bool _applicationmetadataEnabled;

        string _artifactsPath;

        IReadOnlyCollection<IVariable> _buildVariables;
        CancellationToken _cancellationToken;

        bool _cleanBinXmlFilesForAssembliesEnabled;

        bool _cleanWebJobsXmlFilesForAssembliesEnabled;

        bool _codeAnalysisEnabled;
        bool _configurationTransformsEnabled;

        bool _createNuGetWebPackage;
        bool _createWebDeployPackages;
        string _defaultTarget;

        IReadOnlyCollection<string> _excludedNuGetWebPackageFiles;
        IReadOnlyCollection<string> _excludedWebJobsDirectorySegments;

        IReadOnlyCollection<string> _excludedWebJobsFiles;

        IReadOnlyCollection<string> _filteredNuGetWebPackageProjects;

        string _gitHash;
        ILogger _logger;
        string _msBuildExe;
        bool _pdbArtifactsEnabled;
        bool _preCompilationEnabled;
        int _processorCount;

        string _ruleset;
        bool _showSummary;
        string _vcsRoot;
        bool _webProjectsBuildEnabed;
        MSBuildVerbositoyLevel _verbosity;
        ImmutableArray<string> _excludedPlatforms;

        public Guid WebApplicationProjectTypeId { get; } = Guid.Parse("349C5851-65DF-11DA-9384-00065B846F21");

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _buildVariables = buildVariables;
            _logger = logger;
            _cancellationToken = cancellationToken;
            _msBuildExe =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSBuild_ExePath).ThrowIfEmptyValue().Value;
            _artifactsPath =
                buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;

            _appDataJobsEnabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.AppDataJobsEnabled,
                false);

            _webProjectsBuildEnabed =
                buildVariables.GetBooleanByKey(WellKnownVariables.WebProjectsBuildEnabled, true);

            _cleanBinXmlFilesForAssembliesEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.CleanBinXmlFilesForAssembliesEnabled,
                    false);

            _excludedPlatforms = buildVariables
                .GetVariableValueOrDefault(WellKnownVariables.MSBuildExcludedPlatforms, string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToImmutableArray();

            _cleanWebJobsXmlFilesForAssembliesEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.CleanWebJobsXmlFilesForAssembliesEnabled,
                    false);

            _codeAnalysisEnabled =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.ExternalTools_MSBuild_CodeAnalysisEnabled,
                    false);

            _preCompilationEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.WebDeployPreCompilationEnabled, false);

            int maxProcessorCount = ProcessorCount(buildVariables);

            int maxCpuLimit = buildVariables.GetInt32ByKey(
                WellKnownVariables.CpuLimit,
                maxProcessorCount,
                1);

            logger.WriteVerbose($"Using CPU limit: {maxCpuLimit}");

            _processorCount = maxCpuLimit;

            _verbosity =
                MSBuildVerbositoyLevel.TryParse(
                    buildVariables.GetVariableValueOrDefault(
                        WellKnownVariables.ExternalTools_MSBuild_Verbosity,
                        "minimal"));

            _showSummary = buildVariables.GetBooleanByKey(
                WellKnownVariables.ExternalTools_MSBuild_SummaryEnabled,
                true);

            _createWebDeployPackages = buildVariables.GetBooleanByKey(
                WellKnownVariables.WebDeployBuildPackages,
                false);

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

            _configurationTransformsEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.GenericXmlTransformsEnabled, false);
            _defaultTarget =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_MSBuild_DefaultTarget,
                    "rebuild");
            _pdbArtifactsEnabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.PublishPdbFilesAsArtifacts,
                false);
            _createNuGetWebPackage =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.NugetCreateNuGetWebPackagesEnabled,
                    false);
            _gitHash = buildVariables.GetVariableValueOrDefault(WellKnownVariables.GitHash, string.Empty);
            _applicationmetadataEnabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.ApplicationMetadataEnabled,
                false);

            _filteredNuGetWebPackageProjects =
                buildVariables.GetVariableValueOrDefault(
                        WellKnownVariables.NugetCreateNuGetWebPackageFilter,
                        string.Empty)
                    .Split(',')
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .SafeToReadOnlyCollection();

            _excludedWebJobsFiles =
                buildVariables.GetVariableValueOrDefault(
                        WellKnownVariables.WebJobsExcludedFileNameParts,
                        string.Empty)
                    .Split(',')
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .SafeToReadOnlyCollection();

            _excludedNuGetWebPackageFiles =
                buildVariables.GetVariableValueOrDefault(
                        WellKnownVariables.ExcludedNuGetWebPackageFiles,
                        string.Empty)
                    .Split(',')
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .SafeToReadOnlyCollection();

            _excludedWebJobsDirectorySegments =
                buildVariables.GetVariableValueOrDefault(
                        WellKnownVariables.WebJobsExcludedDirectorySegments,
                        string.Empty)
                    .Split(',')
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .SafeToReadOnlyCollection();

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

        string FindRuleSet()
        {
            IReadOnlyCollection<FileInfo> fileInfos = new DirectoryInfo(_vcsRoot)
                .GetFilesRecursive(".ruleset".ValueToImmutableArray(), _pathLookupSpecification, _vcsRoot)
                .ToReadOnlyCollection();

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

        async Task<ExitCode> BuildAsync(ILogger logger, IReadOnlyCollection<IVariable> variables)
        {
            AddBuildConfigurations(logger, variables);

            if (!_buildConfigurations.Any())
            {
                logger.WriteError("No build configurations are defined");
                return ExitCode.Failure;
            }

            AddBuildPlatforms(logger, variables);

            if (!_platforms.Any())
            {
                logger.WriteError("No build platforms are defined");
                return ExitCode.Failure;
            }

            logger.WriteDebug("Starting finding solution files");

            Stopwatch findSolutionFiles = Stopwatch.StartNew();

            IReadOnlyCollection<FileInfo> solutionFiles =
                FindSolutionFiles(new DirectoryInfo(_vcsRoot), logger).ToReadOnlyCollection();

            findSolutionFiles.Stop();

            logger.WriteDebug(
                $"Finding solutions files took {findSolutionFiles.Elapsed.TotalSeconds:F} seconds");

            if (!solutionFiles.Any())
            {
                LogNoSolutionFilesFound(logger);

                return ExitCode.Success;
            }

            IDictionary<FileInfo, IReadOnlyList<string>> solutionPlatforms =
                await GetSolutionPlatformsAsync(solutionFiles);

            logger.WriteVerbose(
                $"Found solutions and platforms: {Environment.NewLine}{string.Join(Environment.NewLine, solutionPlatforms.Select(item => $"{item.Key}: [{string.Join(", ", item.Value)}]"))}");

            foreach (KeyValuePair<FileInfo, IReadOnlyList<string>> solutionPlatform in solutionPlatforms)
            {
                ExitCode result = await BuildSolutionForPlatformAsync(
                    solutionPlatform.Key,
                    solutionPlatform.Value,
                    logger);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return ExitCode.Success;
        }

        async Task<IDictionary<FileInfo, IReadOnlyList<string>>> GetSolutionPlatformsAsync(
            IReadOnlyCollection<FileInfo> solutionFiles)
        {
            IDictionary<FileInfo, IReadOnlyList<string>> solutionPlatforms =
                new Dictionary<FileInfo, IReadOnlyList<string>>();

            foreach (FileInfo solutionFile in solutionFiles)
            {
                List<string> platforms = await GetSolutionPlatformsAsync(solutionFile);

                solutionPlatforms.Add(solutionFile, platforms);
            }

            return solutionPlatforms;
        }

        void LogNoSolutionFilesFound(ILogger logger)
        {
            var messageBuilder = new StringBuilder();

            messageBuilder.Append("Could not find any solution files.");

            var sourceRootDirectories = new DirectoryInfo(_vcsRoot);

            IEnumerable<string> files = sourceRootDirectories.GetFiles().Select(file => file.Name);
            IEnumerable<string> directories = sourceRootDirectories.GetDirectories().Select(dir => dir.Name);

            IEnumerable<string> all = files.Concat(directories);
            messageBuilder.Append(". Root directory files and directories");
            messageBuilder.AppendLine();

            foreach (string item in all)
            {
                messageBuilder.AppendLine(item);
            }

            string message = messageBuilder.ToString();

            logger.WriteWarning(message);
        }

        void AddBuildPlatforms(ILogger logger, IReadOnlyCollection<IVariable> variables)
        {
            string buildPlatform = variables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_MSBuild_BuildPlatform,
                string.Empty);

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
        }

        void AddBuildConfigurations(ILogger logger, IReadOnlyCollection<IVariable> variables)
        {
            string buildConfiguration =
                variables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_MSBuild_BuildConfiguration,
                    string.Empty);

            if (!string.IsNullOrWhiteSpace(buildConfiguration))
            {
                _buildConfigurations.Add(buildConfiguration);
            }
            else
            {
                bool buildDebug = BuildPlatformOrConfiguration(variables, WellKnownVariables.DebugBuildEnabled);

                if (buildDebug)
                {
                    logger.WriteDebug("Adding debug configuration to build");
                    _buildConfigurations.Add("debug");
                }
                else
                {
                    logger.Write($"Flag {WellKnownVariables.DebugBuildEnabled} is set to false, ignoring debug builds");
                }

                bool buildRelease = BuildPlatformOrConfiguration(variables, WellKnownVariables.ReleaseBuildEnabled);

                if (buildRelease)
                {
                    logger.WriteDebug("Adding release configuration to build");
                    _buildConfigurations.Add("release");
                }
                else
                {
                    logger.Write(
                        $"Flag {WellKnownVariables.ReleaseBuildEnabled} is set to false, ignoring release builds");
                }
            }
        }

        bool BuildPlatformOrConfiguration(IReadOnlyCollection<IVariable> variables, string key)
        {
            bool enabled =
                variables.GetBooleanByKey(key, true);

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

                        if (line.IndexOf(
                                "GlobalSection(SolutionConfigurationPlatforms)",
                                StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            isInGlobalSection = true;
                            continue;
                        }

                        if (line.IndexOf(
                                "EndGlobalSection",
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

        async Task<ExitCode> BuildSolutionForPlatformAsync(
            FileInfo solutionFile,
            IReadOnlyList<string> platforms,
            ILogger logger)
        {
            string[] actualPlatforms = platforms
                .Except(_excludedPlatforms, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (actualPlatforms.Length == 0)
            {
                if (_excludedPlatforms.Any())
                {
                    logger.WriteDebug($"Excluding platforms {string.Join(", ", _excludedPlatforms.WrapItems("'"))}");
                }

                logger.WriteWarning($"No platforms are found to be built for solution file '{solutionFile}'");
                return ExitCode.Success;
            }

            var combinations = actualPlatforms
                .SelectMany(
                    item => _buildConfigurations.Select(config => new { Platform = item, Configuration = config }))
                .ToList();

            if (combinations.Count > 1)
            {
                IEnumerable<Dictionary<string, string>> dictionaries =
                    combinations.Select(combination => new Dictionary<string, string>
                    {
                        { "Configuration", combination.Configuration },
                        { "Platform", combination.Platform }
                    });

                logger.WriteVerbose(string.Format(
                    "{0}{0}Configuration/platforms combinations to build: {0}{0}{1}",
                    Environment.NewLine,
                    dictionaries.DisplayAsTable()));
            }

            foreach (string configuration in _buildConfigurations)
            {
                Environment.SetEnvironmentVariable(WellKnownVariables.CurrentBuildConfiguration, configuration);
                ExitCode result =
                    await BuildSolutionWithConfigurationAsync(solutionFile, configuration, logger, actualPlatforms);

                if (!result.IsSuccess)
                {
                    return result;
                }

                Environment.SetEnvironmentVariable(WellKnownVariables.CurrentBuildConfiguration, string.Empty);
            }

            return ExitCode.Success;
        }

        async Task<ExitCode> BuildSolutionWithConfigurationAsync(
            FileInfo solutionFile,
            string configuration,
            ILogger logger,
            IEnumerable<string> platforms)
        {
            foreach (string knownPlatform in platforms)
            {
                Stopwatch buildStopwatch = Stopwatch.StartNew();

                logger.WriteDebug(
                    $"Starting stopwatch for solution file {solutionFile.Name} ({configuration}|{knownPlatform})");

                ExitCode result =
                    await BuildSolutionWithConfigurationAndPlatformAsync(
                        solutionFile,
                        configuration,
                        knownPlatform,
                        logger);

                buildStopwatch.Stop();

                logger.WriteDebug(
                    $"Stopping stopwatch for solution file {solutionFile.Name} ({configuration}|{knownPlatform}), total time in seconds {buildStopwatch.Elapsed.TotalSeconds:F} ({(result.IsSuccess ? "success" : "failed")})");

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
                argList.Add("/property:RunCodeAnalysis=false");
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
                        verboseAction: logger.WriteVerbose,
                        addProcessNameAsLogCategory: true,
                        addProcessRunnerCategory: true);

            if (exitCode.IsSuccess)
            {
                if (_webProjectsBuildEnabed)
                {
                    _logger.Write($"Web projects build is enabled, key {WellKnownVariables.WebProjectsBuildEnabled}");

                    ExitCode webAppsExiteCode =
                        await BuildWebApplicationsAsync(solutionFile, configuration, platform, logger);

                    exitCode = webAppsExiteCode;
                }
                else
                {
                    _logger.Write($"Web projects build is enabled, key {WellKnownVariables.WebProjectsBuildEnabled}");
                }
            }
            else
            {
                logger.WriteError("Skipping web site build since solution build failed");
            }

            CopyCodeAnalysisReportsToArtifacts(configuration, platform, logger);

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

        void CopyCodeAnalysisReportsToArtifacts(string configuration, string platform, ILogger logger)
        {
            IReadOnlyCollection<FileInfo> analysisLogFiles =
                new DirectoryInfo(_vcsRoot).GetFiles("*.AnalysisLog.xml", SearchOption.AllDirectories)
                    .ToReadOnlyCollection();

            DirectoryInfo targetReportDirectory =
                new DirectoryInfo(Path.Combine(_artifactsPath, "CodeAnalysis")).EnsureExists();

            logger.WriteVerbose(
                $"Found {analysisLogFiles.Count} code analysis log files: {string.Join(Environment.NewLine, analysisLogFiles.Select(file => file.FullName))}");

            foreach (FileInfo analysisLogFile in analysisLogFiles)
            {
                string projectName = analysisLogFile.Name.Replace(".CodeAnalysisLog.xml", string.Empty);

                string targetFilePath = Path.Combine(
                    targetReportDirectory.FullName,
                    $"{projectName}.{Platforms.Normalize(platform)}.{configuration}.xml");

                analysisLogFile.CopyTo(targetFilePath);
            }
        }

        Task<ExitCode> PublishPdbFilesAynsc(string configuration, string platform)
        {
            _logger.Write(_pdbArtifactsEnabled
                ? $"Publishing PDB artificats for configuration {configuration} and platform {platform}"
                : $"Skipping PDF artifact publising for configuration {configuration} and platform {platform} because PDB artifact publishing is disabled");

            try
            {
                PathLookupSpecification defaultPathLookupSpecification = DefaultPaths.DefaultPathLookupSpecification;
                IEnumerable<string> ignoredDirectorySegments =
                    defaultPathLookupSpecification.IgnoredDirectorySegments.Except(new[] { "bin" });

                var pathLookupSpecification = new PathLookupSpecification(
                    ignoredDirectorySegments,
                    defaultPathLookupSpecification.IgnoredFileStartsWithPatterns,
                    defaultPathLookupSpecification.IgnoredDirectorySegmentParts,
                    defaultPathLookupSpecification.IgnoredDirectoryStartsWithPatterns);

                var sourceRootDirectory = new DirectoryInfo(_vcsRoot);

                IReadOnlyCollection<FileInfo> files = sourceRootDirectory.GetFilesRecursive(
                        new[] { ".pdb", ".dll" },
                        pathLookupSpecification,
                        _vcsRoot)
                    .OrderBy(file => file.FullName)
                    .ToReadOnlyCollection();

                IReadOnlyCollection<FileInfo> pdbFiles =
                    files.Where(file => file.Extension.Equals(".pdb", StringComparison.InvariantCultureIgnoreCase))
                        .ToReadOnlyCollection();

                IReadOnlyCollection<FileInfo> dllFiles =
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
                                .Equals(
                                    Path.Combine(
                                        pdb.Directory.FullName,
                                        $"{Path.GetFileNameWithoutExtension(pdb.Name)}.dll"),
                                    StringComparison.InvariantCultureIgnoreCase))
                    })
                    .ToReadOnlyCollection();

                string targetDirectoryPath = Path.Combine(
                    _artifactsPath,
                    "PDB",
                    configuration,
                    Platforms.Normalize(platform));

                DirectoryInfo targetDirectory = new DirectoryInfo(targetDirectoryPath).EnsureExists();

                foreach (var pair in pairs)
                {
                    string targetFilePath = Path.Combine(targetDirectory.FullName, pair.PdbFile.Name);

                    if (!File.Exists(targetFilePath))
                    {
                        _logger.WriteDebug($"Copying PDB file '{pair.PdbFile.FullName}' to '{targetFilePath}'");

                        pair.PdbFile.CopyTo(targetFilePath);
                    }
                    else
                    {
                        _logger.WriteDebug($"Target file '{targetFilePath}' already exists, skipping file");
                    }

                    if (pair.DllFile != null)
                    {
                        string targetDllFilePath = Path.Combine(targetDirectory.FullName, pair.DllFile.Name);

                        if (!File.Exists(targetDllFilePath))
                        {
                            _logger.WriteDebug($"Copying DLL file '{pair.DllFile.FullName}' to '{targetFilePath}'");
                            pair.DllFile.CopyTo(targetDllFilePath);
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

        async Task<ExitCode> BuildWebApplicationsAsync(
            FileInfo solutionFile,
            string configuration,
            string platform,
            ILogger logger)
        {
            Solution solution = Solution.LoadFrom(solutionFile.FullName);

            List<SolutionProject> webProjects =
                solution.Projects.Where(
                        project => project.Project.ProjectTypes().Any(type => type == WebApplicationProjectTypeId))
                    .ToList();

            logger.WriteDebug($"Finding WebApplications by looking at project type GUID {WebApplicationProjectTypeId}");

            logger.Write(
                $"WebApplication projects to build [{webProjects.Count}]: {string.Join(", ", webProjects.Select(wp => wp.Project.FileName))}");

            var webSolutionProjects = new List<WebSolutionProject>();

            webSolutionProjects.AddRange(webProjects.Select(project => new WebSolutionProject(
                project.Project.FileName,
                project.Project.ProjectName,
                project.Project.ProjectDirectory,
                project.Project.BuildProject,
                Framework.NetFramework)));

            ImmutableArray<WebSolutionProject> solutionProjects = solution.Projects
                .Where(project => File.ReadAllLines(project.Project.FileName)
                                      .First()
                                      .IndexOf("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(project => new WebSolutionProject(
                    project.Project.FileName,
                    project.Project.ProjectName,
                    project.Project.ProjectDirectory,
                    project.Project.BuildProject,
                    Framework.NetCore))
                .ToImmutableArray();

            webSolutionProjects.AddRange(solutionProjects);

            foreach (WebSolutionProject solutionProject in webSolutionProjects)
            {
                string platformDirectoryPath = Path.Combine(
                    _artifactsPath,
                    "Websites",
                    solutionProject.ProjectName,
                    Platforms.Normalize(platform));

                DirectoryInfo platformDirectory = new DirectoryInfo(platformDirectoryPath).EnsureExists();

                DirectoryInfo siteArtifactDirectory = platformDirectory.CreateSubdirectory(configuration);

                string platformName = Platforms.Normalize(platform);

                ExitCode buildSiteExitCode = await BuildWebApplicationAsync(
                    solutionFile,
                    configuration,
                    logger,
                    solutionProject,
                    platformName,
                    siteArtifactDirectory);

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

                if (_applicationmetadataEnabled)
                {
                    _logger.Write("Application metadata is enabled");
                    await CreateApplicationMetadataAsync(siteArtifactDirectory, platformName, configuration);
                }
                else
                {
                    _logger.Write("Application metadata is disabled");
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

                    ExitCode packageSiteExitCode = await CreateNuGetWebPackagesAsync(
                        logger,
                        platformDirectoryPath,
                        solutionProject,
                        platformName,
                        siteArtifactDirectory.FullName);

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
            }

            return ExitCode.Success;
        }

        Task CreateApplicationMetadataAsync(
            DirectoryInfo siteArtifactDirectory,
            string platformName,
            string configuration)
        {
            var items = new List<KeyValueConfigurationItem>();

            if (!string.IsNullOrWhiteSpace(_gitHash))
            {
                var keyValueConfigurationItem = new KeyValueConfigurationItem(
                    ApplicationMetadataKeys.GitHash,
                    _gitHash,
                    null);
                items.Add(keyValueConfigurationItem);
            }

            string gitBranchName = _buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.BranchLogicalName,
                string.Empty);
            if (!string.IsNullOrWhiteSpace(gitBranchName))
            {
                var keyValueConfigurationItem = new KeyValueConfigurationItem(
                    ApplicationMetadataKeys.GitBranch,
                    gitBranchName,
                    null);
                items.Add(keyValueConfigurationItem);
            }

            var configurationItem = new KeyValueConfigurationItem(
                ApplicationMetadataKeys.DotNetConfiguration,
                configuration,
                null);
            items.Add(configurationItem);

            var cpu = new KeyValueConfigurationItem(ApplicationMetadataKeys.DotNetCpuPlatform, platformName, null);
            items.Add(cpu);

            var configurationItems = new ConfigurationItems(
                "1.0",
                items.Select(i => new KeyValue(i.Key, i.Value, i.ConfigurationMetadata)).ToImmutableArray());
            string serialize = new JsonConfigurationSerializer().Serialize(configurationItems);

            string applicationMetadataJsonFilePath = Path.Combine(
                siteArtifactDirectory.FullName,
                "applicationmetadata.json");

            File.WriteAllText(applicationMetadataJsonFilePath, serialize, Encoding.UTF8);

            string keyPluralSingular = items.Count == 1 ? "key" : "keys";
            string verb = items.Count == 1 ? "has" : "have";

            _logger.Write(
                $"{items.Count} metadata {keyPluralSingular} {verb} been written to '{applicationMetadataJsonFilePath}'");

            return Task.CompletedTask;
        }

        async Task<ExitCode> BuildWebApplicationAsync(
            FileInfo solutionFile,
            string configuration,
            ILogger logger,
            WebSolutionProject solutionProject,
            string platformName,
            DirectoryInfo siteArtifactDirectory)
        {
            List<string> buildSiteArguments;

            if (solutionProject.Framework == Framework.NetFramework)
            {
                buildSiteArguments = new List<string>(15)
                {
                    solutionProject.FullPath,
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

                if (_preCompilationEnabled)
                {
                    _logger.Write($"Pre-compilation is enabled");
                    buildSiteArguments.Add("/property:UseMerge=true");
                    buildSiteArguments.Add("/property:PrecompileBeforePublish=true");
                    buildSiteArguments.Add("/property:SingleAssemblyName=AppCode");
                }
            }
            else
            {
                buildSiteArguments = new List<string>(15)
                {
                    solutionProject.FullPath,
                    $"/property:configuration={configuration}",
                    $"/verbosity:{_verbosity.Level}",
                    "/target:publish",
                    $"/property:publishdir={siteArtifactDirectory.FullName}",
                    $"/maxcpucount:{_processorCount.ToString(CultureInfo.InvariantCulture)}",
                    "/nodeReuse:false"
                };
            }

            if (_showSummary)
            {
                buildSiteArguments.Add("/detailedsummary");
            }

            if (!_codeAnalysisEnabled)
            {
                buildSiteArguments.Add("/property:RunCodeAnalysis=false");
            }

            ExitCode buildSiteExitCode =
                await
                    ProcessRunner.ExecuteAsync(
                        _msBuildExe,
                        arguments: buildSiteArguments,
                        standardOutLog: logger.Write,
                        standardErrorAction: logger.WriteError,
                        toolAction: logger.Write,
                        cancellationToken: _cancellationToken,
                        addProcessNameAsLogCategory: true,
                        addProcessRunnerCategory: true);

            if (buildSiteExitCode.IsSuccess)
            {
                if (_cleanBinXmlFilesForAssembliesEnabled)
                {
                    _logger.WriteDebug("Clean bin directory XML files is enabled");

                    var binDirectory = new DirectoryInfo(Path.Combine(siteArtifactDirectory.FullName, "bin"));

                    if (binDirectory.Exists)
                    {
                        _logger.WriteDebug($"The bin directory '{binDirectory.FullName}' does exist");
                        RemoveXmlFilesForAssemblies(binDirectory);
                    }
                    else
                    {
                        _logger.WriteDebug($"The bin directory '{binDirectory.FullName}' does not exist");
                    }
                }
                else
                {
                    _logger.WriteDebug("Clean bin directory XML files is disabled");
                }
            }

            return buildSiteExitCode;
        }

        async Task<ExitCode> CreateNuGetWebPackagesAsync(
            ILogger logger,
            string platformDirectoryPath,
            WebSolutionProject solutionProject,
            string platformName,
            string siteArtifactDirectory)
        {
            if (
                !platformName.Equals(
                    Platforms.Normalize(WellKnownPlatforms.AnyCPU),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                logger.WriteWarning(
                    $"Only '{WellKnownPlatforms.AnyCPU}' platform is supported for NuGet web packages, skipping platform '{platformName}'");
                return ExitCode.Success;
            }

            string expectedName = string.Format(
                WellKnownVariables.NugetCreateNuGetWebPackageForProjectEnabledFormat,
                solutionProject.ProjectName.Replace(".", "_").Replace(" ", "_").Replace("-", "_"));

            List<MSBuildProperty> msbuildProperties =
                solutionProject.BuildProject.PropertyGroups.SelectMany(s => s.Properties)
                    .Where(
                        msBuildProperty =>
                            msBuildProperty.Name.Equals(expectedName, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

            bool buildNuGetWebPackageForProject = ShouldBuildNuGetWebPackageForProject(
                solutionProject,
                msbuildProperties,
                expectedName);

            if (!buildNuGetWebPackageForProject)
            {
                logger.Write($"Creating NuGet web package for project '{solutionProject.ProjectName}' is disabled");
                return ExitCode.Success;
            }

            logger.Write($"Creating NuGet web package for project '{solutionProject.ProjectName}'");

            string packageId = solutionProject.ProjectName;

            string excluded = _excludedNuGetWebPackageFiles.Any()
                ? $";{string.Join(";", _excludedNuGetWebPackageFiles.Select(excludePattern => $"{siteArtifactDirectory}\\{excludePattern}"))}"
                : string.Empty;

            string files =
                $@"<file src=""{siteArtifactDirectory}\**\*.*"" target=""Content"" exclude=""packages.config{excluded}"" />";

            ExitCode exitCode = await CreateNuGetPackageAsync(
                platformDirectoryPath,
                logger,
                packageId,
                files);

            if (!exitCode.IsSuccess)
            {
                logger.WriteError($"Failed to create NuGet web package for project '{solutionProject.ProjectName}'");
                return exitCode;
            }

            const string environmentLiteral = "Environment";
            const string pattern = "{Name}." + environmentLiteral + ".{EnvironmentName}.{action}.{extension}";
            char separator = '.';
            int fileNameMinPartCount = pattern.Split(separator).Length;

            var environmentFiles = new DirectoryInfo(solutionProject.ProjectDirectory)
                .GetFilesRecursive(rootDir: _vcsRoot)
                .Select(file => new { File = file, Parts = file.Name.Split(separator) })
                .Where(item => item.Parts.Length == fileNameMinPartCount)
                .Where(item => item.Parts[1].Equals(environmentLiteral, StringComparison.OrdinalIgnoreCase))
                .Select(item => new { item.File, EnvironmentName = item.Parts[2] })
                .SafeToReadOnlyCollection();

            IReadOnlyCollection<string> environmentNames = environmentFiles
                .Select(
                    group => new
                    {
                        Key = group.EnvironmentName,
                        InvariantKey = group.EnvironmentName.ToLowerInvariant()
                    })
                .GroupBy(item => item.InvariantKey)
                .Select(grouping => grouping.First().Key)
                .Distinct()
                .SafeToReadOnlyCollection();

            string rootDirectory =
                solutionProject.ProjectDirectory.Trim(Path.DirectorySeparatorChar);

            _logger.WriteVerbose(
                $"Found [{environmentNames.Count}] environnent names in project '{solutionProject.ProjectName}'");

            foreach (string environmentName in environmentNames)
            {
                _logger.WriteVerbose(
                    $"Creating Environment package for project '{solutionProject.ProjectName}', environment name '{environmentName}'");
                List<string> elements = environmentFiles
                    .Select(
                        file =>
                        {
                            string sourceFullPath = file.File.FullName.Trim(Path.DirectorySeparatorChar);
                            string relativePath =
                                sourceFullPath.Replace(rootDirectory, string.Empty).Trim(Path.DirectorySeparatorChar);
                            return new { SourceFullPath = sourceFullPath, RelativePath = relativePath };
                        })
                    .Select(environmentFile =>
                        $"<file src=\"{environmentFile.SourceFullPath}\" target=\"Content\\{environmentFile.RelativePath}\" />")
                    .ToList();

                _logger.WriteVerbose(
                    $"Found '{elements.Count}' environment specific files in project directory '{solutionProject.ProjectDirectory}' environment name '{environmentName}'");

                string environmentPackageId = $"{packageId}.Environment.{environmentName}";

                ExitCode environmentPackageExitCode
                    = await CreateNuGetPackageAsync(
                        platformDirectoryPath,
                        logger,
                        environmentPackageId,
                        string.Join(Environment.NewLine, elements));

                if (!environmentPackageExitCode.IsSuccess)
                {
                    logger.WriteError(
                        $"Failed to create NuGet environment web package for project {solutionProject.ProjectName}");
                    return environmentPackageExitCode;
                }
            }

            logger.Write($"Successfully created NuGet web package for project {solutionProject.ProjectName}");

            return ExitCode.Success;
        }

        bool ShouldBuildNuGetWebPackageForProject(
            WebSolutionProject solutionProject,
            List<MSBuildProperty> msbuildProperties,
            string expectedName)
        {
            bool packageFilterEnabled = _filteredNuGetWebPackageProjects.Any();

            if (packageFilterEnabled)
            {
                _logger.WriteDebug("NuGet Web package filter is enabled");

                string normalizedProjectFileName = Path.GetFileNameWithoutExtension(solutionProject.FullPath);

                bool isIncluded = _filteredNuGetWebPackageProjects.Any(
                    projectName =>
                        projectName.Equals(normalizedProjectFileName, StringComparison.InvariantCultureIgnoreCase));

                _logger.WriteDebug(isIncluded
                    ? $"NuGet Web package for {normalizedProjectFileName} ie enabled by filter"
                    : $"NuGet Web package for {normalizedProjectFileName} is disabled by filter");

                return isIncluded;
            }

            bool buildNuGetWebPackageForProject = true;

            if (msbuildProperties.Any())
            {
                List<ParseResult<bool>> parseResults = msbuildProperties.Select(
                        msBuildProperty =>
                        {
                            ParseResult<bool> parseResult = msBuildProperty.Value.TryParseBool(true);
                            return parseResult;
                        })
                    .Where(item => item.Parsed)
                    .ToList();

                if (parseResults.Any())
                {
                    bool hasAnyPropertySetToFalse = parseResults.Any(item => !item.Value);

                    if (hasAnyPropertySetToFalse)
                    {
                        _logger.WriteVerbose(
                            $"Build NuGet web package is disabled in project file '{solutionProject.FullPath}'; property '{expectedName}'");
                        buildNuGetWebPackageForProject = false;
                    }
                    else
                    {
                        _logger.WriteVerbose(
                            $"Build NuGet web package is enabled via project file '{solutionProject.FullPath}'; property '{expectedName}'");
                    }
                }
                else
                {
                    _logger.WriteDebug(
                        $"Build NuGet web package is not configured in project file '{solutionProject.FullPath}'; property '{expectedName}', invalid value");
                }
            }
            else
            {
                _logger.WriteDebug(
                    $"Build NuGet web package is not configured in project file '{solutionProject.FullPath}'; property '{expectedName}'");
            }

            string buildVariable = _buildVariables.GetVariableValueOrDefault(expectedName, string.Empty);

            if (!string.IsNullOrWhiteSpace(buildVariable))
            {
                ParseResult<bool> parseResult = buildVariable.TryParseBool(true);

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
                _logger.WriteDebug(
                    $"Build NuGet web package is not configured using build variable '{expectedName}', variable is not defined");
            }

            return buildNuGetWebPackageForProject;
        }

        async Task<ExitCode> CreateNuGetPackageAsync(
            string platformDirectoryPath,
            ILogger logger,
            string packageId,
            string filesList)
        {
            const string xmlTemplate = @"<?xml version=""1.0""?>
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
            string authors = _buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.NetAssemblyCompany,
                "Undefined");
            string owners =
                _buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCompany, "Undefined");
            string description = packageId;
            string summary = packageId;
            string language = "en-US";
            string projectUrl = "http://nuget.org";
            string iconUrl = "http://nuget.org";
            string requireLicenseAcceptance = "false";
            string licenseUrl = "http://nuget.org";
            string copyright = _buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.NetAssemblyCopyright,
                "Undefined");
            string tags = string.Empty;

            string files = filesList;

            string nuspecContent = string.Format(
                xmlTemplate,
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
                tags,
                files);

            logger.Write(nuspecContent);

            DirectoryInfo tempDir = new DirectoryInfo(Path.Combine(
                    Path.GetTempPath(),
                    $"{DefaultPaths.TempPathPrefix}_sb_{DateTime.Now:yyyyMMddHHmmssfff_}{Guid.NewGuid().ToString().Substring(0, 8)}"))
                .EnsureExists();

            string nuspecTempFile = Path.Combine(tempDir.FullName, $"{packageId}.nuspec");

            File.WriteAllText(nuspecTempFile, nuspecContent, Encoding.UTF8);

            ExitCode exitCode = await new NuGetPackager(_logger).CreatePackageAsync(
                nuspecTempFile,
                packageConfiguration,
                true,
                _cancellationToken);

            File.Delete(nuspecTempFile);

            tempDir.DeleteIfExists(true);

            return exitCode;
        }

        void TransformFiles(
            string configuration,
            ILogger logger,
            WebSolutionProject solutionProject,
            DirectoryInfo siteArtifactDirectory)
        {
            logger.WriteDebug("Transforms are enabled");

            logger.WriteDebug("Starting xml transformations");

            Stopwatch transformationStopwatch = Stopwatch.StartNew();
            string projectDirectoryPath = solutionProject.ProjectDirectory;

            string[] extensions = { ".xml", ".config" };

            IReadOnlyCollection<FileInfo> files = new DirectoryInfo(projectDirectoryPath)
                .GetFilesRecursive(extensions)
                .Where(
                    file =>
                        !_pathLookupSpecification.IsBlackListed(file.DirectoryName).Item1 &&
                        !_pathLookupSpecification.IsFileBlackListed(file.FullName, _vcsRoot).Item1)
                .Where(
                    file =>
                        extensions.Any(
                            extension =>
                                Path.GetExtension(file.Name)
                                    .Equals(extension, StringComparison.InvariantCultureIgnoreCase)))
                .Where(file => !file.Name.Equals("web.config", StringComparison.InvariantCultureIgnoreCase))
                .ToReadOnlyCollection();

            string TransformFile(FileInfo file)
            {
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
                string extension = Path.GetExtension(file.Name);

                // ReSharper disable once PossibleNullReferenceException
                string transformFilePath = Path.Combine(file.Directory.FullName,
                    nameWithoutExtension + "." + configuration + extension);

                return transformFilePath;
            }

            var transformationPairs = files
                .Select(file => new
                {
                    Original = file,
                    TransformFile = TransformFile(file)
                })
                .Where(filePair => File.Exists(filePair.TransformFile))
                .ToReadOnlyCollection();

            logger.WriteDebug($"Found {transformationPairs.Length} files with transforms");

            foreach (var configurationFile in transformationPairs)
            {
                string relativeFilePath =
                    configurationFile.Original.FullName.Replace(projectDirectoryPath, string.Empty);

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
        async Task<ExitCode> CopyKuduWebJobsAsync(
            [NotNull] ILogger logger,
            [NotNull] WebSolutionProject solutionProject,
            [NotNull] DirectoryInfo siteArtifactDirectory)
        {
            if (solutionProject.Framework != Framework.NetFramework)
            {
                logger.Write("Skipping Kudu web job, only .NET Framework projects supported");
                return ExitCode.Success;
            }

            logger.Write("AppData Web Jobs are enabled");
            logger.WriteDebug("Starting web deploy packaging");

            Stopwatch webJobStopwatch = Stopwatch.StartNew();

            ExitCode exitCode;

            string appDataPath = Path.Combine(solutionProject.ProjectDirectory, "App_Data");

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
                    string artifactJobAppDataPath = Path.Combine(
                        siteArtifactDirectory.FullName,
                        "App_Data",
                        "jobs");

                    DirectoryInfo artifactJobAppDataDirectory =
                        new DirectoryInfo(artifactJobAppDataPath).EnsureExists();

                    logger.WriteVerbose(
                        $"Copying directory '{kuduWebJobs.FullName}' to '{artifactJobAppDataDirectory.FullName}'");

                    IEnumerable<string> ignoredFileNameParts =
                        new[] { ".vshost.", ".CodeAnalysisLog.xml", ".lastcodeanalysissucceeded" }.Concat(
                            _excludedWebJobsFiles);

                    exitCode =
                        await
                            DirectoryCopy.CopyAsync(
                                kuduWebJobs.FullName,
                                artifactJobAppDataDirectory.FullName,
                                logger,
                                rootDir: _vcsRoot,
                                pathLookupSpecificationOption:
                                DefaultPaths.DefaultPathLookupSpecification
                                    .WithIgnoredFileNameParts(ignoredFileNameParts)
                                    .AddExcludedDirectorySegments(_excludedWebJobsDirectorySegments));

                    if (exitCode.IsSuccess)
                    {
                        if (_cleanWebJobsXmlFilesForAssembliesEnabled)
                        {
                            _logger.WriteDebug("Clean bin directory XML files is enabled for WebJobs");

                            var binDirectory = new DirectoryInfo(Path.Combine(artifactJobAppDataDirectory.FullName));

                            if (binDirectory.Exists)
                            {
                                RemoveXmlFilesForAssemblies(binDirectory);
                            }
                        }
                        else
                        {
                            _logger.WriteDebug("Clean bin directory XML files is disabled for WebJobs");
                        }
                    }
                    else
                    {
                        _logger.WriteDebug("Clean bin directory XML files is disabled");
                    }
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

        async Task<ExitCode> CreateWebDeployPackagesAsync(
            FileInfo solutionFile,
            string configuration,
            ILogger logger,
            string platformDirectoryPath,
            WebSolutionProject solutionProject,
            string platformName)
        {
            if (solutionProject.Framework != Framework.NetFramework)
            {
                logger.Write("Skipping web deploy package, only .NET Framework projects supported");
                return ExitCode.Success;
            }

            logger.WriteDebug("Starting web deploy packaging");

            Stopwatch webDeployStopwatch = Stopwatch.StartNew();

            string webDeployPackageDirectoryPath = Path.Combine(platformDirectoryPath, "WebDeploy");

            DirectoryInfo webDeployPackageDirectory = new DirectoryInfo(webDeployPackageDirectoryPath).EnsureExists();

            string packagePath = Path.Combine(
                webDeployPackageDirectory.FullName,
                $"{solutionProject.ProjectName}_{configuration}.zip");

            var buildSitePackageArguments = new List<string>(15)
            {
                solutionProject.FullPath,
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

            if (!_codeAnalysisEnabled)
            {
                buildSitePackageArguments.Add("/property:RunCodeAnalysis=false");
            }

            ExitCode packageSiteExitCode =
                await
                    ProcessRunner.ExecuteAsync(
                        _msBuildExe,
                        arguments: buildSitePackageArguments,
                        standardOutLog: logger.Write,
                        standardErrorAction: logger.WriteError,
                        toolAction: logger.Write,
                        cancellationToken: _cancellationToken,
                        addProcessNameAsLogCategory: true,
                        addProcessRunnerCategory: true);

            webDeployStopwatch.Stop();

            logger.WriteDebug(
                $"WebDeploy packaging took {webDeployStopwatch.Elapsed.TotalSeconds:F} seconds");

            return packageSiteExitCode;
        }

        IEnumerable<FileInfo> FindSolutionFiles(DirectoryInfo directoryInfo, ILogger logger)
        {
            (bool, string) isBlacklisted = IsBlacklisted(directoryInfo);
            if (isBlacklisted.Item1)
            {
                logger.WriteDebug(
                    $"Skipping directory '{directoryInfo.FullName}' when searching for solution files because the directory is blacklisted, {isBlacklisted.Item2}");
                return Enumerable.Empty<FileInfo>();
            }

            List<FileInfo> solutionFiles = directoryInfo.EnumerateFiles("*.sln").ToList();

            foreach (DirectoryInfo subDir in directoryInfo.EnumerateDirectories())
            {
                solutionFiles.AddRange(FindSolutionFiles(subDir, logger));
            }

            return solutionFiles;
        }

        (bool, string) IsBlacklisted(DirectoryInfo directoryInfo)
        {
            (bool, string) isBlacklistedByName =
                _pathLookupSpecification.IsBlackListed(directoryInfo.FullName, _vcsRoot);

            if (isBlacklistedByName.Item1)
            {
                return isBlacklistedByName;
            }

            FileAttributes[] blackListedByAttributes = _blackListedByAttributes.Where(
                blackListed => directoryInfo.Attributes.HasFlag(blackListed)).ToArray();

            bool isBlackListedByAttributes = blackListedByAttributes.Any();

            return (isBlackListedByAttributes, isBlackListedByAttributes
                ? $"Directory has black-listed attributes {string.Join(", ", blackListedByAttributes.Select(_ => Enum.GetName(typeof(FileAttributes), _)))}"
                : string.Empty);
        }

        void RemoveXmlFilesForAssemblies(DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.Exists)
            {
                return;
            }

            _logger.WriteVerbose(
                $"Deleting XML files for corresponding DLL files in directory '{directoryInfo.FullName}'");

            FileInfo[] dllFiles = directoryInfo.GetFiles("*.dll", SearchOption.AllDirectories);

            foreach (FileInfo fileInfo in dllFiles)
            {
                var xmlFile = new FileInfo(Path.Combine(
                    fileInfo.Directory.FullName,
                    $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}.xml"));

                if (xmlFile.Exists)
                {
                    _logger.WriteVerbose($"Deleting XML file '{xmlFile.FullName}'");

                    File.Delete(xmlFile.FullName);

                    _logger.WriteVerbose($"Deleted XML file '{xmlFile.FullName}'");
                }
            }
        }
    }
}
