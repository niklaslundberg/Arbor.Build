using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions;
using Arbor.Build.Core.GenericExtensions.Bools;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Git;
using Arbor.Build.Core.Tools.NuGet;
using Arbor.Defensive.Collections;
using Arbor.Exceptions;
using Arbor.FS;
using Arbor.KVConfiguration.Core.Metadata;
using Arbor.KVConfiguration.Schema.Json;
using Arbor.Processing;
using JetBrains.Annotations;
using Microsoft.Web.XmlTransform;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Zio;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Arbor.Build.Core.Tools.MSBuild;

[Priority(300)]
[UsedImplicitly]
public class SolutionBuilder(
    BuildContext buildContext,
    NuGetPackager nugetPackager,
    IFileSystem fileSystem)
    : ITool, IReportLogTail
{
    private readonly List<FileAttributes> _excludeListedByAttributes =
        [FileAttributes.Hidden, FileAttributes.System, FileAttributes.Offline, FileAttributes.Archive];

    private readonly List<string> _knownPlatforms = ["x86", "x64", "Any CPU"];

    private readonly PathLookupSpecification _pathLookupSpecification =
        DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "node_modules" });

    private readonly List<string> _platforms = [];

    private bool _appDataJobsEnabled;
    private bool _applicationMetadataDotNetConfigurationEnabled;
    private bool _applicationMetadataDotNetCpuPlatformEnabled;

    private bool _applicationMetadataEnabled;
    private bool _applicationMetadataGitBranchEnabled;
    private bool _applicationMetadataGitHashEnabled;
    private MsBuildArgHelper _argHelper = default!;

    private DirectoryEntry _artifactsPath = null!;
    private string _assemblyFileVersion = null!;
    private string _assemblyVersion = null!;
    private BranchName? _branchName;
    private string? _buildSuffix;

    private IReadOnlyCollection<IVariable> _buildVariables = ImmutableArray<IVariable>.Empty;
    private CancellationToken _cancellationToken;

    private bool _cleanBinXmlFilesForAssembliesEnabled;

    private bool _cleanWebJobsXmlFilesForAssembliesEnabled;

    private bool _codeAnalysisEnabled;
    private bool _configurationTransformsEnabled;

    private bool _createNuGetWebPackage;
    private bool _createWebDeployPackages;
    private bool _debugLoggingEnabled;
    private string? _defaultTarget;
    private bool _deterministicBuildEnabled;
    private UPath? _dotNetExePath;
    private bool _dotnetMsBuildEnabled;
    private bool _dotnetPackToolsEnabled;
    private bool _dotnetPublishEnabled = true;

    private IReadOnlyCollection<string> _excludedNuGetWebPackageFiles = ImmutableArray<string>.Empty;
    private ImmutableArray<string> _excludedPlatforms = [];
    private IReadOnlyCollection<string> _excludedWebJobsDirectorySegments = ImmutableArray<string>.Empty;

    private IReadOnlyCollection<string> _excludedWebJobsFiles = ImmutableArray<string>.Empty;

    private IReadOnlyCollection<string> _filteredNuGetWebPackageProjects = ImmutableArray<string>.Empty;

    private string? _gitHash;
    private GitBranchModel? _gitModel;
    private ILogger _logger = Logger.None;
    private bool _logMsBuildWarnings;
    private UPath? _msBuildExe;
    private DirectoryEntry _packagesDirectory = null!;
    private bool _pdbArtifactsEnabled;
    private bool _preCompilationEnabled;
    private int? _processorCount;
    private string? _publishRuntimeIdentifier;
    private string? _publishTargetFramework;

    private UPath? _ruleset;
    private bool _showSummary;
    private DirectoryEntry _vcsRoot = null!;
    private bool _verboseLoggingEnabled;
    private MSBuildVerbosityLevel _verbosity = MSBuildVerbosityLevel.Default;
    private string _version = null!;
    private bool _webProjectsBuildEnabled;
    private bool _assemblyVersionPatchingEnabled;

    public FixedSizedQueue<string> LogTail { get; } = new() { Limit = 5 };

    public async Task<ExitCode> ExecuteAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        string[] args,
        CancellationToken cancellationToken)
    {
        _buildVariables = buildVariables;
        _logger = logger;
        _debugLoggingEnabled = _logger.IsEnabled(LogEventLevel.Debug);
        _verboseLoggingEnabled = _logger.IsEnabled(LogEventLevel.Verbose);
        _cancellationToken = cancellationToken;

        _dotnetMsBuildEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled);

        _assemblyVersionPatchingEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.AssemblyFilePatchingEnabled, true);

        if (_dotnetMsBuildEnabled)
        {
            _logger.Information("SolutionBuilder is using .NET Core MSBuild");
        }
        else
        {
            _logger.Information("SolutionBuilder is using .NET Framework MSBuild");
        }

        _msBuildExe = _dotnetMsBuildEnabled
            ? default
            : buildVariables.Require(WellKnownVariables.ExternalTools_MSBuild_ExePath).GetValueOrThrow()
                .ParseAsPath();

        string msbuildParameterArgumentDelimiter = _dotnetMsBuildEnabled
            ? "-"
            : "/";

        _argHelper = new MsBuildArgHelper(msbuildParameterArgumentDelimiter);

        _artifactsPath =
            new DirectoryEntry(fileSystem,
                buildVariables.Require(WellKnownVariables.Artifacts).GetValueOrThrow().ParseAsPath());

        _appDataJobsEnabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.AppDataJobsEnabled);

        _buildSuffix =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageArtifactsSuffix);

        if (!buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageArtifactsSuffixEnabled, true))
        {
            _buildSuffix = "";
        }

        _version = buildVariables.Require(WellKnownVariables.Version).GetValueOrThrow();

        IVariable artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();
        _packagesDirectory =
            new DirectoryEntry(fileSystem, UPath.Combine(artifacts.Value!.ParseAsPath(), "packages"));

        _dotnetPackToolsEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.DotNetPackToolProjectsEnabled, true);

        _dotnetPublishEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.DotNetPublishExeProjectsEnabled, true);

        _webProjectsBuildEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.WebProjectsBuildEnabled, true);

        _dotNetExePath =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty)?.ParseAsPath();

        _cleanBinXmlFilesForAssembliesEnabled =
            buildVariables.GetBooleanByKey(
                WellKnownVariables.CleanBinXmlFilesForAssembliesEnabled);

        _excludedPlatforms = buildVariables
            .GetVariableValueOrDefault(WellKnownVariables.MSBuildExcludedPlatforms, string.Empty)!
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .ToImmutableArray();

        _cleanWebJobsXmlFilesForAssembliesEnabled =
            buildVariables.GetBooleanByKey(
                WellKnownVariables.CleanWebJobsXmlFilesForAssembliesEnabled);

        _codeAnalysisEnabled =
            buildVariables.GetBooleanByKey(
                WellKnownVariables.ExternalTools_MSBuild_CodeAnalysisEnabled);

        _publishRuntimeIdentifier =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.PublishRuntimeIdentifier, "");

        _publishTargetFramework =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.PublishTargetFramework, "");

        _preCompilationEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.WebDeployPreCompilationEnabled);

        _logMsBuildWarnings =
            buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_LogWarnings, true);

        _verbosity =
            MSBuildVerbosityLevel.TryParse(
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_MSBuild_Verbosity,
                    "minimal"));

        _showSummary = buildVariables.GetBooleanByKey(
            WellKnownVariables.ExternalTools_MSBuild_SummaryEnabled,
            true);

        _createWebDeployPackages = buildVariables.GetBooleanByKey(
            WellKnownVariables.WebDeployBuildPackages);

        if (_verboseLoggingEnabled)
        {
            _logger.Verbose("Using MSBuild verbosity {Verbosity}", _verbosity);
        }

        _vcsRoot = new DirectoryEntry(fileSystem,
            buildVariables.Require(WellKnownVariables.SourceRoot).GetValueOrThrow().ParseAsPath());

        if (_codeAnalysisEnabled)
        {
            _ruleset = FindRuleSet();
        }
        else
        {
            if (_verboseLoggingEnabled)
            {
                _logger.Verbose("Code analysis is disabled, skipping ruleset lookup.");
            }
        }

        _configurationTransformsEnabled =
            buildVariables.GetBooleanByKey(WellKnownVariables.GenericXmlTransformsEnabled);

        _defaultTarget =
            buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_MSBuild_DefaultTarget,
                "restore;rebuild");

        _pdbArtifactsEnabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.PublishPdbFilesAsArtifacts);

        _createNuGetWebPackage =
            buildVariables.GetBooleanByKey(
                WellKnownVariables.NugetCreateNuGetWebPackagesEnabled);

        _gitHash = buildVariables.GetVariableValueOrDefault(WellKnownVariables.GitHash, string.Empty);

        _applicationMetadataEnabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.ApplicationMetadataEnabled);

        _applicationMetadataGitHashEnabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.ApplicationMetadataGitHashEnabled);

        _applicationMetadataGitBranchEnabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.ApplicationMetadataGitBranchEnabled);

        _applicationMetadataDotNetCpuPlatformEnabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.ApplicationMetadataDotNetCpuPlatformEnabled);

        _applicationMetadataDotNetConfigurationEnabled = buildVariables.GetBooleanByKey(
            WellKnownVariables.ApplicationMetadataDotNetConfigurationEnabled);

        _filteredNuGetWebPackageProjects =
            buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.NugetCreateNuGetWebPackageFilter,
                    string.Empty)!
                .Split(',')
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .SafeToReadOnlyCollection();

        _excludedWebJobsFiles =
            buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.WebJobsExcludedFileNameParts,
                    string.Empty)!
                .Split(',')
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .SafeToReadOnlyCollection();

        _excludedNuGetWebPackageFiles =
            buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExcludedNuGetWebPackageFiles,
                    string.Empty)!
                .Split(',')
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .SafeToReadOnlyCollection();

        _excludedWebJobsDirectorySegments =
            buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.WebJobsExcludedDirectorySegments,
                    string.Empty)!
                .Split(',')
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .SafeToReadOnlyCollection();

        _assemblyFileVersion = buildVariables.Require(WellKnownVariables.NetAssemblyFileVersion).Value!;
        _assemblyVersion = buildVariables.Require(WellKnownVariables.NetAssemblyVersion).Value!;
        if (buildVariables.GetInt32ByKey(WellKnownVariables.ExternalTools_MSBuild_CpuCount) is { } cpuCount && cpuCount > 0)
        {
            _processorCount = cpuCount;
        }

        string? gitModel = buildVariables.GetVariableValueOrDefault(WellKnownVariables.GitBranchModel);

        if (GitBranchModel.TryParse(gitModel, out var model))
        {
            _gitModel = model;
        }

        _deterministicBuildEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.DeterministicBuildEnabled);

        var maybe = BranchName.TryParse(buildVariables.GetVariableValueOrDefault(WellKnownVariables.BranchName));

        _branchName = maybe;

        try
        {
            return await BuildAsync(_logger, buildVariables).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            _logger.Error(ex, "Error in solution builder");
            return ExitCode.Failure;
        }
    }

    private static bool BuildPlatformOrConfiguration(IReadOnlyCollection<IVariable> variables, string key)
    {
        bool enabled =
            variables.GetBooleanByKey(key, true);

        return enabled;
    }

    private UPath? FindRuleSet()
    {
        IReadOnlyCollection<FileEntry> rulesetFiles = _vcsRoot
            .GetFilesRecursive(".ruleset".ValueToImmutableArray(), _pathLookupSpecification, _vcsRoot)
            .ToReadOnlyCollection();

        if (rulesetFiles.Count != 1)
        {
            if (rulesetFiles.Count == 0)
            {
                if (_verboseLoggingEnabled)
                {
                    _logger.Verbose("Could not find any ruleset file (.ruleset) in solution");
                }
            }
            else
            {
                if (_verboseLoggingEnabled)
                {
                    _logger.Verbose(
                        "Found {Count} ruleset files (.ruleset) in solution, only one is supported, skipping code analysis with rules",
                        rulesetFiles.Count);
                }
            }

            return null;
        }

        string foundRuleSet = rulesetFiles.Single().FullName;

        if (_verboseLoggingEnabled)
        {
            _logger.Verbose("Found one ruleset file '{FoundRuleSet}'", foundRuleSet);
        }

        return foundRuleSet;
    }

    private async Task<ExitCode> BuildAsync(ILogger logger, IReadOnlyCollection<IVariable> variables)
    {
        if (buildContext.Configurations.Count == 0)
        {
            logger.Error("No build configurations are defined");
            return ExitCode.Failure;
        }

        AddBuildPlatforms(logger, variables);

        if (_platforms.Count == 0)
        {
            logger.Error("No build platforms are defined");
            return ExitCode.Failure;
        }

        if (_debugLoggingEnabled)
        {
            logger.Debug("Starting finding solution files");
        }

        var findSolutionFiles = Stopwatch.StartNew();

        IReadOnlyCollection<FileEntry> solutionFiles =
            FindSolutionFiles(_vcsRoot, logger).ToReadOnlyCollection();

        findSolutionFiles.Stop();
        if (_debugLoggingEnabled)
        {
            logger.Debug("Finding solutions files took {TotalSeconds:F} seconds",
                findSolutionFiles.Elapsed.TotalSeconds);
        }

        if (solutionFiles.Count == 0)
        {
            LogNoSolutionFilesFound(logger);

            return ExitCode.Success;
        }

        IDictionary<FileEntry, IList<string>> solutionPlatforms =
            await GetSolutionPlatformsAsync(solutionFiles).ConfigureAwait(false);

        if (_verboseLoggingEnabled)
        {
            logger.Verbose("Found solutions and platforms: {NewLine}{V}",
                Environment.NewLine,
                string.Join(Environment.NewLine,
                    solutionPlatforms.Select(item => $"{item.Key.ConvertPathToInternal()}: [{string.Join(", ", item.Value)}]")));
        }

        foreach (var solutionPlatform in solutionPlatforms)
        {
            string[] platforms = solutionPlatform.Value.ToArray();

            foreach (string platform in platforms)
            {
                if (!_platforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
                {
                    solutionPlatform.Value.Remove(platform);
                    logger.Debug("Removing found platform {Platform} found in file {SolutionFile}",
                        platform,
                        solutionPlatform.Key.ConvertPathToInternal());
                }
            }
        }

        KeyValuePair<FileEntry, IList<string>>[] filteredPlatforms = solutionPlatforms
            .Where(s => s.Value.Count > 0)
            .ToArray();

        if (filteredPlatforms.Length == 0)
        {
            logger.Error("Could not find any solution platforms");
            return ExitCode.Failure;
        }

        foreach (var solutionPlatform in filteredPlatforms)
        {
            var result = await BuildSolutionForPlatformAsync(
                solutionPlatform.Key,
                solutionPlatform.Value,
                logger).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result;
            }
        }

        return ExitCode.Success;
    }

    private async Task<List<string>> GetSolutionPlatformsAsync(FileEntry solutionFile)
    {
        var platforms = new List<string>();

        await using (var fs = solutionFile.Open(FileMode.Open, FileAccess.Read))
        {
            using var streamReader = new StreamReader(fs);
            bool isInGlobalSection = false;

            while (streamReader.Peek() >= 0)
            {
                string? line = await streamReader.ReadLineAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.Contains("GlobalSection(SolutionConfigurationPlatforms)",
                        StringComparison.OrdinalIgnoreCase))
                {
                    isInGlobalSection = true;
                    continue;
                }

                if (line.Contains("EndGlobalSection", StringComparison.OrdinalIgnoreCase))
                {
                    isInGlobalSection = false;
                    continue;
                }

                if (isInGlobalSection)
                {
                    platforms.AddRange(_platforms.Where(knownPlatform =>
                        line.Contains(knownPlatform, StringComparison.InvariantCulture)));
                }
            }
        }

        return platforms.Distinct().ToList();
    }

    private async Task<IDictionary<FileEntry, IList<string>>> GetSolutionPlatformsAsync(
        IReadOnlyCollection<FileEntry> solutionFiles)
    {
        IDictionary<FileEntry, IList<string>> solutionPlatforms =
            new Dictionary<FileEntry, IList<string>>();

        foreach (FileEntry solutionFile in solutionFiles)
        {
            List<string> platforms = await GetSolutionPlatformsAsync(solutionFile).ConfigureAwait(false);

            solutionPlatforms.Add(solutionFile, platforms);
        }

        return solutionPlatforms;
    }

    private void LogNoSolutionFilesFound(ILogger logger)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.Append("Could not find any solution files.");

        IEnumerable<string> files = _vcsRoot.GetFiles().Select(file => file.Name);
        IEnumerable<string> directories = _vcsRoot.GetDirectories().Select(dir => dir.Name);

        IEnumerable<string> all = files.Concat(directories);
        messageBuilder.Append(". Root directory files and directories");
        messageBuilder.AppendLine();

        foreach (string item in all)
        {
            messageBuilder.AppendLine(item);
        }

        string message = messageBuilder.ToString();

        logger.Warning("{Message}", message);
    }

    private void AddBuildPlatforms(ILogger logger, IReadOnlyCollection<IVariable> variables)
    {
        string? buildPlatform = variables.GetVariableValueOrDefault(
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
                logger.Information("Flag {IgnoreAnyCpu} is set, ignoring AnyCPU builds",
                    WellKnownVariables.IgnoreAnyCpu);
                _platforms.Remove("Any CPU");
            }
        }
    }


    private async Task<ExitCode> BuildSolutionForPlatformAsync(
        FileEntry solutionFile,
        IList<string> platforms,
        ILogger logger)
    {
        string[] actualPlatforms = platforms
            .Except(_excludedPlatforms, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (actualPlatforms.Length == 0)
        {
            if (_excludedPlatforms.Any() && _debugLoggingEnabled)
            {
                logger.Debug("Excluding platforms {Platforms}",
                    string.Join(", ", _excludedPlatforms.WrapItems("'")));
            }

            logger.Warning("No platforms are found to be built for solution file '{SolutionFile}'", solutionFile);
            return ExitCode.Success;
        }

        var combinations = actualPlatforms
            .SelectMany(
                item => buildContext.Configurations.Select(config =>
                    new { Platform = item, Configuration = config }))
            .ToList();

        if (combinations.Count > 1)
        {
            IEnumerable<Dictionary<string, string?>> dictionaries =
                combinations.Select(combination => new Dictionary<string, string?>
                {
                    {"Configuration", combination.Configuration}, {"Platform", combination.Platform}
                });

            if (_verboseLoggingEnabled)
            {
                logger.Verbose(
                    "{NewLine}{NewLine1}Configuration/platforms combinations to build: {NewLine2}{NewLine3}{V}",
                    Environment.NewLine,
                    Environment.NewLine,
                    Environment.NewLine,
                    Environment.NewLine,
                    dictionaries.DisplayAsTable());
            }
        }

        foreach (string configuration in buildContext.Configurations)
        {
            buildContext.CurrentBuildConfiguration = new BuildConfiguration(configuration);

            var result =
                await BuildSolutionWithConfigurationAsync(solutionFile, configuration, logger, actualPlatforms)
                    .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return result;
            }
        }

        return ExitCode.Success;
    }

    private async Task<ExitCode> BuildSolutionWithConfigurationAsync(
        FileEntry solutionFile,
        string configuration,
        ILogger logger,
        IEnumerable<string> platforms)
    {
        foreach (string knownPlatform in platforms)
        {
            var buildStopwatch = Stopwatch.StartNew();
            if (_debugLoggingEnabled)
            {
                logger.Debug("Starting stopwatch for solution file {Name} ({Configuration}|{KnownPlatform})",
                    solutionFile.Name,
                    configuration,
                    knownPlatform);
            }

            var result =
                await BuildSolutionWithConfigurationAndPlatformAsync(
                    solutionFile,
                    configuration,
                    knownPlatform,
                    logger).ConfigureAwait(false);

            buildStopwatch.Stop();
            if (_debugLoggingEnabled)
            {
                logger.Debug(
                    "Stopping stopwatch for solution file {Name} ({Configuration}|{KnownPlatform}), total time in seconds {TotalSeconds:F} ({V})",
                    solutionFile.Name,
                    configuration,
                    knownPlatform,
                    buildStopwatch.Elapsed.TotalSeconds,
                    result.IsSuccess ? "success" : "failed");
            }

            if (!result.IsSuccess)
            {
                logger.Error(
                    "Could not build solution file {FullName} with configuration {Configuration} and platform {KnownPlatform}",
                    fileSystem.ConvertPathToInternal(solutionFile.FullName),
                    configuration,
                    knownPlatform);
                return result;
            }
        }

        return ExitCode.Success;
    }

    private async Task<ExitCode> BuildSolutionWithConfigurationAndPlatformAsync(
        FileEntry solutionFile,
        string configuration,
        string platform,
        ILogger logger)
    {
        if (!_dotnetMsBuildEnabled && _msBuildExe is null)
        {
            logger.Error("MSBuild path is not defined");
            return ExitCode.Failure;
        }

        if (!_dotnetMsBuildEnabled && _msBuildExe.HasValue && !fileSystem.FileExists(_msBuildExe.Value))
        {
            logger.Error("The MSBuild path '{MsBuildExe}' does not exist", _msBuildExe);
            return ExitCode.Failure;
        }

        bool isReleaseBuild =
            configuration.Equals(WellKnownConfigurations.Release, StringComparison.OrdinalIgnoreCase);

        var options = GetVersionOptions(isReleaseBuild);

        string packageVersion = NuGetVersionHelper.GetPackageVersion(options);

        var argList = new List<string>(10)
        {
            fileSystem.ConvertPathToInternal(solutionFile.FullName),
            _argHelper.FormatPropertyArg("configuration", configuration),
            _argHelper.FormatPropertyArg("platform", platform),
            _argHelper.FormatArg("verbosity", _verbosity.Level),
            _argHelper.FormatArg("target", _defaultTarget),
        };

        if (_assemblyVersionPatchingEnabled)
        {
            argList.Add(_argHelper.FormatPropertyArg("AssemblyVersion", _assemblyVersion));
            argList.Add(_argHelper.FormatPropertyArg("FileVersion", _assemblyFileVersion));
            argList.Add(_argHelper.FormatPropertyArg("Version", packageVersion));
        }

        if (_deterministicBuildEnabled)
        {
            argList.Add(_argHelper.FormatPropertyArg("ContinuousIntegrationBuild", "true"));
        }

        if (_processorCount is >= 1)
        {
            argList.Add(_argHelper.FormatArg("maxcpucount",
                _processorCount.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (!_logMsBuildWarnings)
        {
            argList.Add(_argHelper.FormatArg("clp", "ErrorsOnly"));
        }

        if (_codeAnalysisEnabled)
        {
            if (_verboseLoggingEnabled)
            {
                logger.Verbose("Code analysis is enabled");
            }

            argList.Add(_argHelper.FormatPropertyArg("RunCodeAnalysis", "true"));

            if (_ruleset is { } && fileSystem.FileExists(_ruleset.Value))
            {
                logger.Information("Using code analysis ruleset '{Ruleset}'", _ruleset);

                argList.Add(_argHelper.FormatPropertyArg("CodeAnalysisRuleSet",
                    fileSystem.ConvertPathToInternal(_ruleset.Value)));
            }
        }
        else
        {
            argList.Add(_argHelper.FormatPropertyArg("RunCodeAnalysis", "false"));
            logger.Information("Code analysis is disabled");
        }

        if (_showSummary)
        {
            argList.Add(_argHelper.FormatArg("detailedsummary"));
        }

        logger.Information("Building solution file {Name} ({Configuration}|{Platform})",
            solutionFile.Name,
            configuration,
            platform);

        if (_verboseLoggingEnabled)
        {
            logger.Verbose("{NewLine}MSBuild arguments: {NewLine1}{NewLine2}{V}",
                Environment.NewLine,
                Environment.NewLine,
                Environment.NewLine,
                argList.Select(arg => new Dictionary<string, string?> { { "Value", arg } }).DisplayAsTable());
        }

        var verboseAction =
            _verboseLoggingEnabled ? logger.Verbose : (CategoryLog?)null;
        var debugAction = _verboseLoggingEnabled ? logger.Debug : (CategoryLog?)null;

        void LogDefault(string message, string category)
        {
            if (message.Trim().Contains("): warning ", StringComparison.OrdinalIgnoreCase))
            {
                if (_logMsBuildWarnings)
                {
                    logger.Warning("{Category} {Message}", category, message);
                    LogTail.Enqueue($"{category} {message}");
                }
            }
            else
            {
                logger.Information("{Category} {Message}", category, message);
                LogTail.Enqueue($"{category} {message}");
            }
        }

        var exePath = AdjustBuildArgs(argList);

        var exitCode =
            await
                ProcessRunner.ExecuteProcessAsync(
                        fileSystem.ConvertPathToInternal(exePath),
                        argList,
                        LogDefault,
                        logger.Error,
                        debugAction,
                        debugAction: debugAction,
                        cancellationToken: _cancellationToken,
                        verboseAction: verboseAction)
                    .ConfigureAwait(false);

        if (exitCode.IsSuccess)
        {
            exitCode = await PublishProjectsAsync(solutionFile, configuration, logger)
                .ConfigureAwait(false);
        }
        else
        {
            logger.Error("Skipping dotnet publish exe projects because solution build failed");
        }


        if (exitCode.IsSuccess)
        {
            if (_webProjectsBuildEnabled)
            {
                _logger.Information("Web projects builds are enabled, key {WebProjectsBuildEnabled}",
                    WellKnownVariables.WebProjectsBuildEnabled);

                var webAppsExitCode =
                    await BuildWebApplicationsAsync(solutionFile, configuration, platform, logger)
                        .ConfigureAwait(false);

                exitCode = webAppsExitCode;
            }
            else
            {
                _logger.Information("Web projects builds are disabled, key {WebProjectsBuildEnabled}",
                    WellKnownVariables.WebProjectsBuildEnabled);
            }
        }
        else
        {
            logger.Error("Skipping web site build since solution build failed");
        }

        if (exitCode.IsSuccess)
        {
            if (_dotnetPackToolsEnabled)
            {
                _logger.Information("Dotnet pack tools are enabled, key {Key}",
                    WellKnownVariables.DotNetPackToolProjectsEnabled);

                var webAppsExitCode =
                    await PackDotNetProjectsAsync(solutionFile, configuration, logger)
                        .ConfigureAwait(false);

                exitCode = webAppsExitCode;
            }
            else
            {
                _logger.Information("Dotnet pack tools are disabled, key {Key}",
                    WellKnownVariables.DotNetPackToolProjectsEnabled);
            }
        }
        else
        {
            logger.Error("Skipping dotnet publish exe projects because solution build failed");
        }

        CopyCodeAnalysisReportsToArtifacts(configuration, platform, logger);

        if (exitCode.IsSuccess)
        {
            exitCode = await PublishPdbFilesAsync(configuration, platform).ConfigureAwait(false);
        }
        else
        {
            logger.Error("Skipping PDB publishing since web site build failed");
        }

        return exitCode;
    }

    private UPath AdjustBuildArgs(List<string> argList)
    {
        bool dotnetMsBuildEnabled = _dotnetMsBuildEnabled || (_msBuildExe.HasValue &&
                                                              _msBuildExe.Value.FullName.EndsWith("dotnet.exe",
                                                                  StringComparison.OrdinalIgnoreCase));

        if (dotnetMsBuildEnabled
            && argList.Count > 0
            && !argList[0].Equals("msbuild", StringComparison.OrdinalIgnoreCase))
        {
            argList.Insert(0, "msbuild");

            return _dotNetExePath!.Value;
        }

        return _msBuildExe!.Value;
    }

    private async Task<ExitCode> PublishProjectsAsync(FileEntry solutionFile, string configuration, ILogger logger)
    {
        var exitCode = ExitCode.Success;

        if (_dotnetPublishEnabled)
        {
            _logger.Information("Dotnet publish is enabled, key {Key}",
                WellKnownVariables.DotNetPublishExeProjectsEnabled);

            var webAppsExitCode =
                await PublishDotNetProjectsAsync(solutionFile, configuration, logger)
                    .ConfigureAwait(false);

            exitCode = webAppsExitCode;
        }
        else
        {
            _logger.Information("Dotnet publish is disabled, key {Key}",
                WellKnownVariables.DotNetPublishExeProjectsEnabled);
        }

        return exitCode;
    }

    private async Task<ExitCode> PublishDotNetProjectsAsync(
        FileEntry solutionFile,
        string configuration,
        ILogger logger)
    {
        if (_dotNetExePath is null)
        {
            logger.Warning("dotnet could not be found, skipping publishing dotnet exe projects");
            return ExitCode.Success;
        }

        Solution solution = await Solution.LoadFrom(solutionFile);

        var publishProjects = solution.Projects
            .Where(project => project.PublishEnabled())
            .ToImmutableArray();

        foreach (SolutionProject solutionProject in publishProjects)
        {
            var packageLookupDirectories = new List<DirectoryEntry>();

            var targetFrameworks = string.IsNullOrWhiteSpace(_publishTargetFramework)
                ? solutionProject.Project.TargetFrameworks
                : solutionProject.Project.TargetFrameworks.Where(target =>
                    target.Value.Equals(_publishTargetFramework, StringComparison.Ordinal));

            bool isReleaseBuild =
                configuration.Equals(WellKnownConfigurations.Release, StringComparison.OrdinalIgnoreCase);

            var options = GetVersionOptions(isReleaseBuild);

            string packageVersion = NuGetVersionHelper.GetPackageVersion(options);

            DirectoryEntry? tempDirectory = default;

            try
            {
                var tempDirPath =
                    UPath.Combine(Path.GetTempPath().ParseAsPath(),
                        "Arbor.Build-pkg" + DateTime.UtcNow.Ticks);
                tempDirectory = new DirectoryEntry(fileSystem, tempDirPath);
                tempDirectory.EnsureExists();

                packageLookupDirectories.Add(solutionProject.ProjectDirectory);
                packageLookupDirectories.Add(tempDirectory);

                foreach (var targetFramework in targetFrameworks)
                {
                    if (solutionProject.Project.HasPropertyWithValue(WellKnownVariables.DotNetPublishExeEnabled,
                            "false"))
                    {
                        if (logger.IsEnabled(LogEventLevel.Debug))
                        {
                            logger.Debug(
                                "Skipping publish of project {Project} because it has property {Property} set to false",
                                solutionProject.FullPath,
                                WellKnownVariables.DotNetPublishExeEnabled);
                        }

                        continue;
                    }

                    var args = new List<string>
                    {
                        "publish", solutionProject.FullPath.ConvertPathToInternal(), "-c", configuration
                    };

                    if (!logger.IsEnabled(LogEventLevel.Debug))
                    {
                        args.Add("--verbosity");
                        args.Add("minimal");
                    }

                    if (_deterministicBuildEnabled)
                    {
                        args.Add("-p:ContinuousIntegrationBuild=true");
                    }

                    if (solutionProject.HasPublishPackageEnabled())
                    {
                        if (_assemblyVersionPatchingEnabled)
                        {
                            args.Add(_argHelper.FormatPropertyArg("version", packageVersion));
                        }

                        args.Add("--output");

                        args.Add(tempDirectory.ConvertPathToInternal());
                    }

                    string? runtimeIdentifier = solutionProject.Project.GetPropertyValue("RuntimeIdentifier").WithDefault(_publishRuntimeIdentifier);

                    if (!string.IsNullOrWhiteSpace(runtimeIdentifier))
                    {
                        args.Add("-r");
                        args.Add(runtimeIdentifier);
                    }

                    args.Add("-f");
                    args.Add(targetFramework.Value);

                    void Log(string message, string category)
                    {
                        if (message.Trim().Contains("): warning ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (_logMsBuildWarnings)
                            {
                                logger.Warning("{Category} {Message}", category, message);
                            }
                        }
                        else
                        {
                            logger.Information("{Category} {Message}", category, message);
                        }
                    }

                    var projectExitCode = await ProcessRunner.ExecuteProcessAsync(
                        fileSystem.ConvertPathToInternal(_dotNetExePath.Value),
                        args,
                        Log,
                        cancellationToken: _cancellationToken).ConfigureAwait(false);

                    if (!projectExitCode.IsSuccess)
                    {
                        return projectExitCode;
                    }
                }
            }
            finally
            {
                _packagesDirectory.EnsureExists();

                foreach (var lookupDirectory in packageLookupDirectories)
                {
                    if (lookupDirectory is { Exists: true })
                    {
                        var nugetPackages = lookupDirectory.GetFiles($"*{packageVersion}.nupkg",
                            SearchOption.AllDirectories);

                        foreach (var nugetPackage in nugetPackages)
                        {
                            var targetFilePath = _packagesDirectory.Path / nugetPackage.Name;

                            if (!fileSystem.FileExists(targetFilePath))
                            {
                                nugetPackage.CopyTo(targetFilePath, true);
                            }
                        }

                        var nugetSymbolPackages = lookupDirectory.GetFiles($"*{packageVersion}.snupkg",
                            SearchOption.AllDirectories);

                        foreach (var nugetPackage in nugetSymbolPackages)
                        {
                            var targetFile = UPath.Combine(_packagesDirectory.Path, nugetPackage.Name);

                            if (!fileSystem.FileExists(targetFile))
                            {
                                nugetPackage.CopyTo(targetFile, true);
                            }
                        }
                    }
                }

                try
                {
                    tempDirectory.DeleteIfExists();
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Could not remove directory {Directory}", tempDirectory?.FullName);
                }
            }
        }


        return ExitCode.Success;
    }

    private VersionOptions GetVersionOptions(bool isReleaseBuild)
    {
        var options = new VersionOptions(_version)
        {
            BuildNumberEnabled = true,
            BuildSuffix = _buildSuffix,
            IsReleaseBuild = isReleaseBuild,
            Logger = _logger,
            GitModel = _gitModel,
            BranchName = _branchName
        };

        return options;
    }

    private async Task<ExitCode> PackDotNetProjectsAsync(
        FileEntry solutionFile,
        string configuration,
        ILogger logger)
    {
        if (_dotNetExePath is null)
        {
            logger.Warning("dotnet could not be found, skipping publishing dotnet tool projects");
            return ExitCode.Success;
        }

        static bool IsPackageProject(SolutionProject project)
        {
            return project.NetFrameworkGeneration == NetFrameworkGeneration.NetCoreApp
                   && project.Project.HasPropertyWithValue("OutputType", "Exe")
                   && project.Project.HasPropertyWithValue("PackAsTool", "true");
        }

        Solution solution = await Solution.LoadFrom(solutionFile);

        var exeProjects = solution.Projects.Where(IsPackageProject).ToImmutableArray();

        bool isReleaseBuild =
            configuration.Equals(WellKnownConfigurations.Release, StringComparison.OrdinalIgnoreCase);

        var options = GetVersionOptions(isReleaseBuild);
        string packageVersion = NuGetVersionHelper.GetPackageVersion(options);

        foreach (SolutionProject solutionProject in exeProjects)
        {
            EnsureFileDates(solutionProject.ProjectDirectory);

            string[] args =
            [
                "pack", fileSystem.ConvertPathToInternal(solutionProject.FullPath.Path), "--configuration",
                configuration, _argHelper.FormatPropertyArg("VersionPrefix", packageVersion), "--output",
                fileSystem.ConvertPathToInternal(_packagesDirectory.Path)
            ];

            void Log(string message, string category)
            {
                if (message.Trim().Contains("): warning ", StringComparison.OrdinalIgnoreCase))
                {
                    if (_logMsBuildWarnings)
                    {
                        logger.Warning("{Category} {Message}", category, message);
                    }
                }
                else
                {
                    logger.Information("{Category} {Message}", category, message);
                }
            }

            var projectExitCode = await ProcessRunner.ExecuteProcessAsync(
                fileSystem.ConvertPathToInternal(_dotNetExePath.Value),
                args,
                Log,
                cancellationToken: _cancellationToken).ConfigureAwait(false);

            if (!projectExitCode.IsSuccess)
            {
                return projectExitCode;
            }
        }

        return ExitCode.Success;
    }

    private void EnsureFileDates(DirectoryEntry? directory)
    {
        if (directory is null)
        {
            return;
        }

        var files = directory.EnumerateFiles();

        foreach (var file in files)
        {
            try
            {
                file.EnsureHasValidDate(_logger);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not ensure dates for file '{File}'", file.FullName);
            }
        }

        foreach (var subDirectory in directory.EnumerateDirectories())
        {
            EnsureFileDates(subDirectory);
        }
    }

    private void CopyCodeAnalysisReportsToArtifacts(string configuration, string platform, ILogger logger)
    {
        IReadOnlyCollection<FileEntry> analysisLogFiles =
            _vcsRoot.GetFiles("*.AnalysisLog.xml", SearchOption.AllDirectories)
                .ToReadOnlyCollection();

        DirectoryEntry targetReportDirectory =
            new DirectoryEntry(fileSystem, UPath.Combine(_artifactsPath.Path, "CodeAnalysis")).EnsureExists();

        if (_verboseLoggingEnabled)
        {
            logger.Verbose("Found {Count} code analysis log files: {V}",
                analysisLogFiles.Count,
                string.Join(Environment.NewLine, analysisLogFiles.Select(file => file.FullName)));
        }

        foreach (FileEntry analysisLogFile in analysisLogFiles)
        {
            string projectName = analysisLogFile.Name.Replace(".CodeAnalysisLog.xml", string.Empty,
                StringComparison.OrdinalIgnoreCase);

            var targetFilePath = UPath.Combine(
                targetReportDirectory.FullName,
                $"{projectName}.{Platforms.Normalize(platform)}.{configuration}.xml");

            analysisLogFile.CopyTo(targetFilePath, true);
        }
    }

    private Task<ExitCode> PublishPdbFilesAsync(string configuration, string platform)
    {
        string message = _pdbArtifactsEnabled
            ? $"Publishing PDB artifacts for configuration {configuration} and platform {platform}"
            : $"Skipping PDF artifact publishing for configuration {configuration} and platform {platform} because PDB artifact publishing is disabled";

        _logger.Information("{Message}", message);

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

            IReadOnlyCollection<FileEntry> files = _vcsRoot.GetFilesRecursive(
                    new[] { ".pdb", ".dll" },
                    pathLookupSpecification,
                    _vcsRoot)
                .OrderBy(file => file.FullName)
                .ToReadOnlyCollection();

            IReadOnlyCollection<FileEntry> pdbFiles =
                files.Where(file => file.ExtensionWithDot?.Equals(".pdb", StringComparison.OrdinalIgnoreCase) ?? false)
                    .ToReadOnlyCollection();

            IReadOnlyCollection<FileEntry> dllFiles =
                files.Where(file => file.ExtensionWithDot?.Equals(".dll", StringComparison.OrdinalIgnoreCase) ?? false)
                    .ToReadOnlyCollection();
            if (_debugLoggingEnabled)
            {
                _logger.Debug("Found files as PDB artifacts {V}",
                    string.Join(Environment.NewLine, pdbFiles.Select(file => "\tPDB: " + file.ConvertPathToInternal())));
            }

            var pairs = pdbFiles
                .Select(pdb => new
                {
                    PdbFile = pdb,
                    DllFile = dllFiles
                        .SingleOrDefault(dll => dll.FullName
                            .Equals(
                                UPath.Combine(
                                    pdb.Directory.FullName,
                                    $"{Path.GetFileNameWithoutExtension(pdb.Name)}.dll").FullName,
                                StringComparison.OrdinalIgnoreCase))
                })
                .ToReadOnlyCollection();

            var targetDirectoryPath = UPath.Combine(
                _artifactsPath.Path,
                "PDB",
                configuration,
                Platforms.Normalize(platform));

            DirectoryEntry targetDirectory = new DirectoryEntry(fileSystem, targetDirectoryPath).EnsureExists();

            foreach (var pair in pairs)
            {
                var targetFilePath = UPath.Combine(targetDirectory.FullName, pair.PdbFile.Name);

                if (!fileSystem.FileExists(targetFilePath))
                {
                    if (_debugLoggingEnabled)
                    {
                        _logger.Debug("Copying PDB file '{FullName}' to '{TargetFilePath}'",
                            pair.PdbFile.ConvertPathToInternal(),
                            fileSystem.ConvertPathToInternal(targetFilePath));
                    }

                    pair.PdbFile.CopyTo(targetFilePath, true);
                }
                else
                {
                    if (_debugLoggingEnabled)
                    {
                        _logger.Debug("Target file '{TargetFilePath}' already exists, skipping file",
                            fileSystem.ConvertPathToInternal(targetFilePath));
                    }
                }

                if (pair.DllFile is not null)
                {
                    var targetDllFilePath = UPath.Combine(targetDirectory.FullName, pair.DllFile.Name);

                    if (!fileSystem.FileExists(targetDllFilePath))
                    {
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("Copying DLL file '{FullName}' to '{TargetFilePath}'",
                                pair.DllFile.ConvertPathToInternal(),
                                fileSystem.ConvertPathToInternal(targetFilePath));
                        }

                        pair.DllFile.CopyTo(targetDllFilePath, true);
                    }
                    else
                    {
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("Target DLL file '{TargetDllFilePath}' already exists, skipping file",
                                fileSystem.ConvertPathToInternal(targetDllFilePath));
                        }
                    }
                }
                else
                {
                    if (_debugLoggingEnabled)
                    {
                        _logger.Debug("DLL file for PDB '{FullName}' was not found", pair.PdbFile.ConvertPathToInternal());
                    }
                }
            }
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            _logger.Error(ex, "Could not publish PDB artifacts.");
            return Task.FromResult(ExitCode.Failure);
        }

        return Task.FromResult(ExitCode.Success);
    }

    private async Task<ExitCode> BuildWebApplicationsAsync(
        FileEntry solutionFile,
        string configuration,
        string platform,
        ILogger logger)
    {
        Solution solution = await Solution.LoadFrom(solutionFile);

        var webProjects = solution.Projects
            .Where(project => project.Project.ProjectTypes.Any(type => type == ProjectType.Mvc5))
            .ToList();

        if (_debugLoggingEnabled)
        {
            logger.Debug("Finding WebApplications by looking at project type GUID {WebApplicationProjectTypeId}",
                ProjectType.Mvc5);
        }

        logger.Information("WebApplication projects to build [{Count}]: {Projects}",
            webProjects.Count,
            string.Join(", ", webProjects.Select(wp => fileSystem.ConvertPathToInternal(wp.Project.FileName.Path))));

        var webSolutionProjects = new List<WebSolutionProject>();

        webSolutionProjects.AddRange(webProjects.Select(project => new WebSolutionProject(
            project.Project.FileName,
            project.Project.ProjectName,
            project.Project.ProjectDirectory,
            project.Project,
            NetFrameworkGeneration.NetFramework)));

        async Task<bool> IsWebSdkProject(FileEntry file)
        {
            var stream = file.Open(FileMode.Open, FileAccess.Read);

            await foreach (string line in stream.EnumerateLinesAsync().WithCancellation(_cancellationToken))
            {
                if (line.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                break;
            }

            return false;
        }

        var webProjectsItems = new List<SolutionProject>();

        foreach (var solutionProject in solution.Projects)
        {
            if (await IsWebSdkProject(solutionProject.FullPath))
            {
                webProjectsItems.Add(solutionProject);
            }
        }

        var solutionProjects = webProjectsItems
            .Select(project => new WebSolutionProject(
                project.Project.FileName,
                project.Project.ProjectName,
                project.Project.ProjectDirectory,
                project.Project,
                NetFrameworkGeneration.NetCoreApp))
            .ToImmutableArray();

        webSolutionProjects.AddRange(solutionProjects);

        foreach (WebSolutionProject solutionProject in webSolutionProjects)
        {
            var platformDirectoryPath = UPath.Combine(
                _artifactsPath.Path,
                "Websites",
                solutionProject.ProjectName,
                Platforms.Normalize(platform));

            DirectoryEntry platformDirectory =
                new DirectoryEntry(fileSystem, platformDirectoryPath).EnsureExists();

            DirectoryEntry siteArtifactDirectory = platformDirectory.CreateSubdirectory(configuration);

            string platformName = Platforms.Normalize(platform);

            var buildSiteExitCode = await BuildWebApplicationAsync(
                solutionFile,
                configuration,
                logger,
                solutionProject,
                platformName,
                siteArtifactDirectory).ConfigureAwait(false);

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
                if (_debugLoggingEnabled)
                {
                    logger.Debug("Transforms are disabled");
                }
            }

            if (_applicationMetadataEnabled)
            {
                _logger.Information("Application metadata is enabled");
                await CreateApplicationMetadataAsync(siteArtifactDirectory, platformName, configuration)
                    .ConfigureAwait(false);
            }
            else
            {
                _logger.Information("Application metadata is disabled");
            }

            if (_appDataJobsEnabled)
            {
                logger.Information("AppData Web Jobs are enabled");

                var exitCode = await CopyKuduWebJobsAsync(logger, solutionProject, siteArtifactDirectory)
                    .ConfigureAwait(false);

                if (!exitCode.IsSuccess)
                {
                    return exitCode;
                }
            }
            else
            {
                logger.Information("AppData Web Jobs are disabled");
            }

            if (_createWebDeployPackages)
            {
                logger.Information("Web Deploy package creation is enabled, creating package for {ProjectName}",
                    solutionProject.ProjectName);

                var packageSiteExitCode =
                    await
                        CreateWebDeployPackagesAsync(
                            solutionFile,
                            configuration,
                            logger,
                            platformDirectoryPath,
                            solutionProject,
                            platformName).ConfigureAwait(false);

                if (!packageSiteExitCode.IsSuccess)
                {
                    return packageSiteExitCode;
                }
            }
            else
            {
                logger.Information("Web Deploy package creation is disabled");
            }

            if (_createNuGetWebPackage)
            {
                logger.Information(
                    "NuGet web package creation is enabled, creating NuGet package for {ProjectName}",
                    solutionProject.ProjectName);

                var packageSiteExitCode = await CreateNuGetWebPackagesAsync(
                    logger,
                    platformDirectoryPath,
                    solutionProject,
                    platformName,
                    siteArtifactDirectory.FullName).ConfigureAwait(false);

                if (!packageSiteExitCode.IsSuccess)
                {
                    return packageSiteExitCode;
                }
            }
            else
            {
                logger.Debug(
                    "NuGet web package creation is disabled, build variable '{NugetCreateNuGetWebPackagesEnabled}' is not set or value is other than true",
                    WellKnownVariables.NugetCreateNuGetWebPackagesEnabled);
            }
        }

        return ExitCode.Success;
    }

    private async Task CreateApplicationMetadataAsync(
        DirectoryEntry siteArtifactDirectory,
        string platformName,
        string configuration)
    {
        var items = new List<KeyValueConfigurationItem>();

        if (!string.IsNullOrWhiteSpace(_gitHash) && _applicationMetadataGitHashEnabled)
        {
            var keyValueConfigurationItem = new KeyValueConfigurationItem(
                ApplicationMetadataKeys.GitHash,
                _gitHash,
                null);
            items.Add(keyValueConfigurationItem);
        }

        string? gitBranchName = _buildVariables.GetVariableValueOrDefault(
            WellKnownVariables.BranchLogicalName,
            string.Empty);

        if (!string.IsNullOrWhiteSpace(gitBranchName) && _applicationMetadataGitBranchEnabled)
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

        if (!string.IsNullOrWhiteSpace(gitBranchName) && _applicationMetadataDotNetConfigurationEnabled)
        {
            items.Add(configurationItem);
        }

        var cpu = new KeyValueConfigurationItem(ApplicationMetadataKeys.DotNetCpuPlatform, platformName, null);

        if (!string.IsNullOrWhiteSpace(gitBranchName) && _applicationMetadataDotNetCpuPlatformEnabled)
        {
            items.Add(cpu);
        }

        var configurationItems = new ConfigurationItems(
            "1.0",
            items.Select(i => new KeyValue(i.Key, i.Value, i.ConfigurationMetadata)).ToImmutableArray());
        string serialize = JsonConfigurationSerializer.Serialize(configurationItems);

        UPath applicationMetadataJsonFilePath;

        const string applicationmetadataJson = "applicationmetadata.json";

        if (fileSystem.DirectoryExists(UPath.Combine(siteArtifactDirectory.Path, "wwwroot")))
        {
            applicationMetadataJsonFilePath = UPath.Combine(
                siteArtifactDirectory.Path,
                "wwwroot",
                applicationmetadataJson);
        }
        else
        {
            applicationMetadataJsonFilePath = UPath.Combine(
                siteArtifactDirectory.Path,
                applicationmetadataJson);
        }

        new DirectoryEntry(fileSystem, applicationMetadataJsonFilePath.GetDirectory()).EnsureExists();

        await using var stream = fileSystem.OpenFile(applicationMetadataJsonFilePath, FileMode.OpenOrCreate, FileAccess.Write);

        await stream.WriteAllTextAsync(serialize, cancellationToken: _cancellationToken);

        string keyPluralSingular = items.Count == 1 ? "key" : "keys";
        string verb = items.Count == 1 ? "has" : "have";

        _logger.Information(
            "{Count} metadata {KeyPluralSingular} {Verb} been written to '{ApplicationMetadataJsonFilePath}'",
            items.Count,
            keyPluralSingular,
            verb,
            applicationMetadataJsonFilePath);
    }

    private async Task<ExitCode> BuildWebApplicationAsync(
        FileEntry solutionFile,
        string configuration,
        ILogger logger,
        WebSolutionProject solutionProject,
        string platformName,
        DirectoryEntry siteArtifactDirectory)
    {
        List<string> buildSiteArguments;

        string target;

        if (solutionProject.NetFrameworkGeneration == NetFrameworkGeneration.NetFramework)
        {
            target = "pipelinePreDeployCopyAllFilesToOneFolder";

            buildSiteArguments = new List<string>(15)
            {
                fileSystem.ConvertPathToInternal(solutionProject.FullPath.Path),
                _argHelper.FormatPropertyArg("configuration", configuration),
                _argHelper.FormatPropertyArg("platform", platformName),
                _argHelper.FormatPropertyArg("_PackageTempDir", fileSystem.ConvertPathToInternal(siteArtifactDirectory.FullName)),

                // ReSharper disable once PossibleNullReferenceException
                _argHelper.FormatPropertyArg("SolutionDir", fileSystem.ConvertPathToInternal(solutionFile.Directory.FullName)),
                _argHelper.FormatArg("verbosity", _verbosity.Level),
                _argHelper.FormatPropertyArg("AutoParameterizationWebConfigConnectionStrings", "false")
            };

            if (_preCompilationEnabled)
            {
                _logger.Information("Pre-compilation is enabled");
                buildSiteArguments.Add(_argHelper.FormatPropertyArg("UseMerge", "true"));
                buildSiteArguments.Add(_argHelper.FormatPropertyArg("PrecompileBeforePublish", "true"));
                buildSiteArguments.Add(_argHelper.FormatPropertyArg("SingleAssemblyName", "AppCode"));
            }
        }
        else
        {
            buildSiteArguments = new List<string>(15)
            {
                fileSystem.ConvertPathToInternal(solutionProject.FullPath.Path),
                _argHelper.FormatPropertyArg("configuration", configuration),
                _argHelper.FormatArg("verbosity", _verbosity.Level),
                _argHelper.FormatPropertyArg("publishdir", fileSystem.ConvertPathToInternal(siteArtifactDirectory.FullName)),
            };

            if (_assemblyVersionPatchingEnabled)
            {
                buildSiteArguments.Add(_argHelper.FormatPropertyArg("AssemblyVersion", _assemblyVersion));
                buildSiteArguments.Add(_argHelper.FormatPropertyArg("FileVersion", _assemblyFileVersion));
                buildSiteArguments.Add(_argHelper.FormatPropertyArg("Version", _version));
            }

            if (_deterministicBuildEnabled)
            {
                buildSiteArguments.Add(_argHelper.FormatPropertyArg("ContinuousIntegrationBuild", "true"));
            }

            string rid =
                solutionProject.Project.GetPropertyValue(WellKnownVariables.ProjectMSBuildPublishRuntimeIdentifier);

            if (!string.IsNullOrWhiteSpace(rid))
            {
                buildSiteArguments.Add(_argHelper.FormatPropertyArg("RuntimeIdentifier", rid));
            }

            target = "restore;rebuild;publish";
        }

        if (_processorCount is >= 1)
        {
            buildSiteArguments.Add(_argHelper.FormatArg("maxcpucount",
                _processorCount.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (_showSummary)
        {
            buildSiteArguments.Add(_argHelper.FormatArg("detailedsummary"));
        }

        buildSiteArguments.Add(_argHelper.FormatArg("target", target));

        if (!_codeAnalysisEnabled)
        {
            buildSiteArguments.Add(_argHelper.FormatPropertyArg("RunCodeAnalysis", "false"));
        }

        var exePath = AdjustBuildArgs(buildSiteArguments);

        var buildSiteExitCode =
            await
                ProcessRunner.ExecuteProcessAsync(
                    fileSystem.ConvertPathToInternal(exePath),
                    buildSiteArguments,
                    logger.Information,
                    logger.Error,
                    logger.Information,
                    cancellationToken: _cancellationToken).ConfigureAwait(false);

        if (buildSiteExitCode.IsSuccess)
        {
            if (_cleanBinXmlFilesForAssembliesEnabled)
            {
                if (_debugLoggingEnabled)
                {
                    _logger.Debug("Clean bin directory XML files is enabled");
                }

                var binDirectory =
                    new DirectoryEntry(fileSystem, UPath.Combine(siteArtifactDirectory.FullName, "bin"));

                if (binDirectory.Exists)
                {
                    if (_debugLoggingEnabled)
                    {
                        _logger.Debug("The bin directory '{FullName}' does exist", fileSystem.ConvertPathToInternal(binDirectory.FullName));
                    }

                    RemoveXmlFilesForAssemblies(binDirectory);
                }
                else
                {
                    if (_debugLoggingEnabled)
                    {
                        _logger.Debug("The bin directory '{FullName}' does not exist", fileSystem.ConvertPathToInternal(binDirectory.FullName));
                    }
                }
            }
            else
            {
                if (_debugLoggingEnabled)
                {
                    _logger.Debug("Clean bin directory XML files is disabled");
                }
            }
        }

        return buildSiteExitCode;
    }

    private async Task<ExitCode> CreateNuGetWebPackagesAsync(
        ILogger logger,
        UPath platformDirectoryPath,
        WebSolutionProject solutionProject,
        string platformName,
        string siteArtifactDirectory)
    {
        if (
            !platformName.Equals(
                Platforms.Normalize(WellKnownPlatforms.AnyCPU),
                StringComparison.OrdinalIgnoreCase))
        {
            logger.Warning(
                "Only '{AnyCPU}' platform is supported for NuGet web packages, skipping platform '{PlatformName}'",
                WellKnownPlatforms.AnyCPU,
                platformName);
            return ExitCode.Success;
        }

        string expectedName = string.Format(CultureInfo.InvariantCulture,
            WellKnownVariables.NugetCreateNuGetWebPackageForProjectEnabledFormat,
            solutionProject.ProjectName
                .Replace(".", "_", StringComparison.InvariantCulture)
                .Replace(" ", "_", StringComparison.InvariantCulture)
                .Replace("-", "_", StringComparison.InvariantCulture));

        var msbuildProperties =
            solutionProject.Project.PropertyGroups.SelectMany(s => s.Properties)
                .Where(
                    msBuildProperty =>
                        msBuildProperty.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase)
                        || msBuildProperty.Name.Equals(
                            WellKnownVariables.NugetCreateNuGetWebPackageForProjectEnabled,
                            StringComparison.Ordinal))
                .ToList();

        bool buildNuGetWebPackageForProject = ShouldBuildNuGetWebPackageForProject(
            solutionProject,
            msbuildProperties,
            expectedName);

        if (!buildNuGetWebPackageForProject)
        {
            logger.Information("Creating NuGet web package for project '{ProjectName}' is disabled",
                solutionProject.ProjectName);
            return ExitCode.Success;
        }

        logger.Information("Creating NuGet web package for project '{ProjectName}'", solutionProject.ProjectName);

        string packageId = solutionProject.ProjectName;

        var artifactDirectory = new DirectoryEntry(fileSystem, siteArtifactDirectory);

        var allIncludedFiles =
            artifactDirectory.GetFilesWithWithExclusions(_excludedNuGetWebPackageFiles);

        var exitCode = await CreateNuGetPackageAsync(
            platformDirectoryPath,
            logger,
            packageId,
            allIncludedFiles,
            "",
            artifactDirectory,
            runtimeIdentifier: _publishRuntimeIdentifier).ConfigureAwait(false);

        if (!exitCode.IsSuccess)
        {
            logger.Error("Failed to create NuGet web package for project '{ProjectName}'",
                solutionProject.ProjectName);
            return exitCode;
        }

        const string environmentLiteral = "Environment";
        const string pattern = "{Name}." + environmentLiteral + ".{EnvironmentName}.{action}.{extension}";
        const char separator = '.';
        int fileNameMinPartCount = pattern.Split(separator).Length;

        var environmentFiles = solutionProject.ProjectDirectory
            .GetFilesRecursive(rootDir: _vcsRoot)
            .Select(file => new { File = file, Parts = file.Name.Split(separator) })
            .Where(item => item.Parts.Length == fileNameMinPartCount
                           && item.Parts[1].Equals(environmentLiteral, StringComparison.OrdinalIgnoreCase))
            .Select(item => new { item.File, EnvironmentName = item.Parts[2] })
            .SafeToReadOnlyCollection();

        IReadOnlyCollection<string> environmentNames = environmentFiles
            .Select(
                group => new { Key = group.EnvironmentName, InvariantKey = group.EnvironmentName.ToLowerInvariant() })
            .GroupBy(item => item.InvariantKey)
            .Select(grouping => grouping.First().Key)
            .Distinct()
            .SafeToReadOnlyCollection();

        DirectoryEntry rootDirectory = solutionProject.ProjectDirectory;

        if (_verboseLoggingEnabled)
        {
            _logger.Verbose("Found [{Count}] environment names in project '{ProjectName}'",
                environmentNames.Count,
                solutionProject.ProjectName);
        }

        foreach (string environmentName in environmentNames)
        {
            if (_verboseLoggingEnabled)
            {
                _logger.Verbose(
                    "Creating Environment package for project '{ProjectName}', environment name '{EnvironmentName}'",
                    solutionProject.ProjectName,
                    environmentName);
            }

            var elements = environmentFiles
                .Where(file => file.EnvironmentName.Equals(environmentName, StringComparison.OrdinalIgnoreCase))
                .Select(file => file.File)
                .ToList();

            if (_verboseLoggingEnabled)
            {
                _logger.Verbose(
                    "Found '{Count}' environment specific files in project directory '{ProjectDirectory}' environment name '{EnvironmentName}'",
                    elements.Count,
                    solutionProject.ProjectDirectory,
                    environmentName);
            }

            string environmentPackageId = $"{packageId}";

            var environmentPackageExitCode = await CreateNuGetPackageAsync(
                platformDirectoryPath,
                logger,
                environmentPackageId,
                elements,
                $".Environment.{environmentName}",
                rootDirectory,
                runtimeIdentifier: _publishRuntimeIdentifier).ConfigureAwait(false);

            if (!environmentPackageExitCode.IsSuccess)
            {
                logger.Error("Failed to create NuGet environment web package for project {ProjectName}",
                    solutionProject.ProjectName);
                return environmentPackageExitCode;
            }
        }

        logger.Information("Successfully created NuGet web package for project {ProjectName}",
            solutionProject.ProjectName);

        return ExitCode.Success;
    }

    private bool ShouldBuildNuGetWebPackageForProject(
        WebSolutionProject solutionProject,
        List<MSBuildProperty> msbuildProperties,
        string expectedName)
    {
        bool packageFilterEnabled = _filteredNuGetWebPackageProjects.Count > 0;

        if (packageFilterEnabled)
        {
            if (_debugLoggingEnabled)
            {
                _logger.Debug("NuGet Web package filter is enabled");
            }

            string normalizedProjectFileName = solutionProject.FullPath.NameWithoutExtension;

            bool isIncluded = _filteredNuGetWebPackageProjects.Any(
                projectName =>
                    projectName.Equals(normalizedProjectFileName, StringComparison.OrdinalIgnoreCase));

            if (_debugLoggingEnabled)
            {
                string message = isIncluded
                    ? $"NuGet Web package for {normalizedProjectFileName} ie enabled by filter"
                    : $"NuGet Web package for {normalizedProjectFileName} is disabled by filter";

                _logger.Debug("{Message}", message);
            }

            return isIncluded;
        }

        bool buildNuGetWebPackageForProject = true;

        if (msbuildProperties.Count > 0)
        {
            bool hasAnyPropertySetToFalse =
                msbuildProperties.Any(property => bool.TryParse(property.Value, out bool result) && !result);

            if (hasAnyPropertySetToFalse)
            {
                if (_verboseLoggingEnabled)
                {
                    _logger.Verbose(
                        "Build NuGet web package is disabled in project file '{FullPath}'; property '{ExpectedName}'",
                        solutionProject.FullPath,
                        expectedName);
                }

                buildNuGetWebPackageForProject = false;
            }
            else
            {
                if (_verboseLoggingEnabled)
                {
                    _logger.Verbose(
                        "Build NuGet web package is enabled via project file '{FullPath}'; property '{ExpectedName}'",
                        solutionProject.FullPath,
                        expectedName);
                }
            }
        }
        else
        {
            if (_debugLoggingEnabled)
            {
                _logger.Debug(
                    "Build NuGet web package is not configured in project file '{FullPath}'; property '{ExpectedName}'",
                    solutionProject.FullPath.ConvertPathToInternal(),
                    expectedName);
            }
        }

        string? buildVariable = _buildVariables.GetVariableValueOrDefault(expectedName, string.Empty);

        if (!string.IsNullOrWhiteSpace(buildVariable))
        {
            bool parsed = buildVariable.TryParseBool(out bool parseResult, true);

            if (parsed && !parseResult)
            {
                if (_verboseLoggingEnabled)
                {
                    _logger.Verbose("Build NuGet web package is turned off in build variable '{ExpectedName}'",
                        expectedName);
                }

                buildNuGetWebPackageForProject = false;
            }
            else if (parsed)
            {
                if (_debugLoggingEnabled)
                {
                    _logger.Debug("Build NuGet web package is enabled in build variable '{ExpectedName}'",
                        expectedName);
                }
            }
            else
            {
                if (_debugLoggingEnabled)
                {
                    _logger.Debug("Build NuGet web package is not configured in build variable '{ExpectedName}'",
                        expectedName);
                }
            }
        }
        else
        {
            if (_debugLoggingEnabled)
            {
                _logger.Debug(
                    "Build NuGet web package is not configured using build variable '{ExpectedName}', variable is not defined",
                    expectedName);
            }
        }

        return buildNuGetWebPackageForProject;
    }

    private async Task<ExitCode> CreateNuGetPackageAsync(
        UPath platformDirectoryPath,
        ILogger logger,
        string packageId,
        IReadOnlyCollection<FileEntry> filesList,
        string packageNameSuffix,
        DirectoryEntry baseDirectory,
        string? runtimeIdentifier = null)
    {
        var packageDirectoryPath = UPath.Combine(platformDirectoryPath, "NuGet");

        DirectoryEntry packageDirectory = new DirectoryEntry(fileSystem, packageDirectoryPath).EnsureExists();

        var packageConfiguration = nugetPackager.GetNuGetPackageConfiguration(
            logger,
            _buildVariables,
            packageDirectory,
            _vcsRoot,
            packageNameSuffix,
            runtimeIdentifier);

        if (packageConfiguration is null)
        {
            return ExitCode.Success;
        }

        packageConfiguration.NuGetSymbolPackagesEnabled = false;

        string name = packageId;

        string? authors = _buildVariables.GetVariableValueOrDefault(
            WellKnownVariables.NetAssemblyCompany,
            "Undefined");
        string? owners =
            _buildVariables.GetVariableValueOrDefault(WellKnownVariables.NetAssemblyCompany, "Undefined");
        string description = packageId;
        string summary = packageId;
        const string language = "en-US";
        const string projectUrl = "http://nuget.org";
        const string iconUrl = "http://nuget.org";
        const string requireLicenseAcceptance = "false";
        string? copyright = _buildVariables.GetVariableValueOrDefault(
            WellKnownVariables.NetAssemblyCopyright,
            "Undefined");
        string tags = string.Empty;

        string files = string.Join(Environment.NewLine,
            filesList.Select(file => NuSpecHelper.IncludedFile(file, baseDirectory, _logger)));

        FileListWithChecksumFile contentFilesInfo = await ChecksumHelper.CreateFileListForDirectory(baseDirectory);

        string nativeMetadataDirectory = contentFilesInfo.ContentFilesFile.Directory.Path.WindowsPath();

        string nativePath = contentFilesInfo.ContentFilesFile.Path.WindowsPath();
        string nativeChecksumPath = contentFilesInfo.ChecksumFile.Path.WindowsPath();

        string nativeFullPath = fileSystem.ConvertPathToInternal(contentFilesInfo.ContentFilesFile.Path);
        string nativeChecksumFileFullPath = fileSystem.ConvertPathToInternal(contentFilesInfo.ChecksumFile.Path);

        string contentFileListFile =
            $@"<file src=""{nativeFullPath}"" target=""{nativePath[nativeMetadataDirectory.Length..].TrimStart(Path.DirectorySeparatorChar)}"" />";
        string checksumFile =
            $@"<file src=""{nativeChecksumFileFullPath}"" target=""{nativeChecksumPath[nativeMetadataDirectory.Length..].TrimStart(Path.DirectorySeparatorChar)}"" />";

        string nuspecContent = $@"<?xml version=""1.0""?>
<package>
    <metadata>
        <id>{name}</id>
        <version>{packageConfiguration.Version.ToNormalizedString()}</version>
        <title>{name}</title>
        <authors>{authors}</authors>
        <owners>{owners}</owners>
        <description>
            {description}
        </description>
        <releaseNotes>
        </releaseNotes>
        <summary>
            {summary}
        </summary>
        <language>{language}</language>
        <projectUrl>{projectUrl}</projectUrl>
        <iconUrl>{iconUrl}</iconUrl>
        <requireLicenseAcceptance>{requireLicenseAcceptance}</requireLicenseAcceptance>
        <copyright>{copyright}</copyright>
        <dependencies>

        </dependencies>
        <references></references>
        <tags>{tags}</tags>
    </metadata>
    <files>
        {files}
        {contentFileListFile}
        {checksumFile}
    </files>
</package>";

        logger.Information("{NuSpec}", nuspecContent);

        DirectoryEntry tempDir = new DirectoryEntry(fileSystem, UPath.Combine(
                Path.GetTempPath().ParseAsPath(),
                $"{DefaultPaths.TempPathPrefix}_sb_{DateTime.Now.Ticks}"))
            .EnsureExists();

        var nuspecTempFile = UPath.Combine(tempDir.Path, $"{packageId}.nuspec");

        await WriteNuSpec(nuspecTempFile, nuspecContent);

        var packageSpecificationPath = new FileEntry(fileSystem, nuspecTempFile);
        var exitCode = await nugetPackager.CreatePackageAsync(
            packageSpecificationPath,
            packageConfiguration,
            true,
            _cancellationToken).ConfigureAwait(false);

        packageSpecificationPath.DeleteIfExists();
        contentFilesInfo.ContentFilesFile.Directory.DeleteIfExists();
        tempDir.DeleteIfExists();

        return exitCode;
    }

    private async Task WriteNuSpec(UPath nuspecTempFile, string nuspecContent)
    {
        await using var stream = fileSystem.OpenFile(nuspecTempFile, FileMode.OpenOrCreate, FileAccess.Write);

        await stream
            .WriteAllTextAsync(nuspecContent, Encoding.UTF8, cancellationToken: _cancellationToken)
            .ConfigureAwait(false);
    }

    private void TransformFiles(
        string configuration,
        ILogger logger,
        WebSolutionProject solutionProject,
        DirectoryEntry siteArtifactDirectory)
    {
        if (_debugLoggingEnabled)
        {
            logger.Debug("Transforms are enabled");

            logger.Debug("Starting xml transformations");
        }

        var transformationStopwatch = Stopwatch.StartNew();
        var projectDirectoryPath = solutionProject.ProjectDirectory;

        string[] extensions = [".xml", ".config"];

        IReadOnlyCollection<FileEntry> files = projectDirectoryPath
            .GetFilesRecursive(extensions)
            .Where(
                file =>
                    !_pathLookupSpecification.IsNotAllowed(file.Directory).Item1
                    && !_pathLookupSpecification.IsFileExcluded(file, _vcsRoot).Item1)
            .Where(
                file =>
                    extensions.Any(
                        extension =>
                            Path.GetExtension(file.Name)
                                .Equals(extension, StringComparison.OrdinalIgnoreCase))
                    && !file.Name.Equals("web.config", StringComparison.OrdinalIgnoreCase))
            .ToImmutableArray();

        UPath TransformFile(FileEntry file)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
            string extension = Path.GetExtension(file.Name);

            // ReSharper disable once PossibleNullReferenceException
            var transformFilePath = UPath.Combine(file.Directory.FullName,
                $"{nameWithoutExtension}.{configuration}{extension}");

            return transformFilePath;
        }

        var transformationPairs = files
            .Select(file => new { Original = file, TransformFile = TransformFile(file) })
            .Where(filePair => fileSystem.FileExists(filePair.TransformFile))
            .ToReadOnlyCollection();
        if (_debugLoggingEnabled)
        {
            logger.Debug("Found {Length} files with transforms", transformationPairs.Length);
        }

        foreach (var configurationFile in transformationPairs)
        {
            string relativeFilePath =
                configurationFile.Original.FullName.Replace(projectDirectoryPath.FullName,
                    string.Empty,
                    StringComparison.InvariantCulture);

            string targetTransformResultPath = $"{siteArtifactDirectory.FullName}{relativeFilePath}";

            using var transformable = new XmlTransformableDocument();

            transformable.Load(fileSystem.ConvertPathToInternal(configurationFile.Original.Path));

            using var transformation =
                new XmlTransformation(fileSystem.ConvertPathToInternal(configurationFile.TransformFile));
            if (_debugLoggingEnabled)
            {
                logger.Debug(
                    "Transforming '{FullName}' with transformation file '{TransformFile} to target file '{TargetTransformResultPath}'",
                    configurationFile.Original.FullName,
                    configurationFile.TransformFile,
                    targetTransformResultPath);
            }

            if (transformation.Apply(transformable))
            {
                transformable.Save(targetTransformResultPath);
            }
        }

        transformationStopwatch.Stop();
        if (_debugLoggingEnabled)
        {
            logger.Debug("XML transformations took {Seconds} seconds",
                transformationStopwatch.Elapsed.TotalSeconds.ToString("F", CultureInfo.InvariantCulture));
        }
    }

    private async Task<ExitCode> CopyKuduWebJobsAsync(
        ILogger logger,
        WebSolutionProject solutionProject,
        DirectoryEntry siteArtifactDirectory)
    {
        if (solutionProject.NetFrameworkGeneration != NetFrameworkGeneration.NetFramework)
        {
            logger.Information("Skipping Kudu web job, only .NET Framework projects supported");
            return ExitCode.Success;
        }

        logger.Information("AppData Web Jobs are enabled");
        if (_debugLoggingEnabled)
        {
            logger.Debug("Starting web deploy packaging");
        }

        var webJobStopwatch = Stopwatch.StartNew();

        ExitCode exitCode;

        var appDataPath = UPath.Combine(solutionProject.ProjectDirectory.Path, "App_Data");

        var appDataDirectory = new DirectoryEntry(fileSystem, appDataPath);

        if (appDataDirectory.Exists)
        {
            if (_verboseLoggingEnabled)
            {
                logger.Verbose("Site has App_Data directory: '{FullName}'", appDataDirectory.FullName);
            }

            DirectoryEntry? kuduWebJobs =
                appDataDirectory.EnumerateDirectories()
                    .SingleOrDefault(
                        directory =>
                            directory.Name.Equals("jobs", StringComparison.OrdinalIgnoreCase));

            if (kuduWebJobs?.Exists == true)
            {
                if (_verboseLoggingEnabled)
                {
                    logger.Verbose("Site has App_Data jobs directory: '{FullName}'", kuduWebJobs.FullName);
                }

                var artifactJobAppDataPath = UPath.Combine(
                    siteArtifactDirectory.FullName,
                    "App_Data",
                    "jobs");

                DirectoryEntry artifactJobAppDataDirectory =
                    new DirectoryEntry(fileSystem, artifactJobAppDataPath).EnsureExists();

                if (_verboseLoggingEnabled)
                {
                    logger.Verbose("Copying directory '{FullName}' to '{FullName1}'",
                        kuduWebJobs.FullName,
                        artifactJobAppDataDirectory.FullName);
                }

                IEnumerable<string> ignoredFileNameParts =
                    new[] { ".vshost.", ".CodeAnalysisLog.xml", ".lastcodeanalysissucceeded" }.Concat(
                        _excludedWebJobsFiles);

                exitCode =
                    await
                        DirectoryCopy.CopyAsync(
                                kuduWebJobs,
                                artifactJobAppDataDirectory,
                                logger,
                                DefaultPaths.DefaultPathLookupSpecification
                                    .WithIgnoredFileNameParts(ignoredFileNameParts)
                                    .AddExcludedDirectorySegments(_excludedWebJobsDirectorySegments),
                                _vcsRoot)
                            .ConfigureAwait(false);

                if (exitCode.IsSuccess)
                {
                    if (_cleanWebJobsXmlFilesForAssembliesEnabled)
                    {
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("Clean bin directory XML files is enabled for WebJobs");
                        }

                        var binDirectory = new DirectoryEntry(fileSystem,
                            UPath.Combine(artifactJobAppDataDirectory.Path, "bin"));

                        if (binDirectory.Exists)
                        {
                            RemoveXmlFilesForAssemblies(binDirectory);
                        }
                    }
                    else
                    {
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("Clean bin directory XML files is disabled for WebJobs");
                        }
                    }
                }
                else
                {
                    if (_debugLoggingEnabled)
                    {
                        _logger.Debug("Clean bin directory XML files is disabled");
                    }
                }
            }
            else
            {
                if (_verboseLoggingEnabled)
                {
                    logger.Verbose("Site has no jobs directory in App_Data directory: '{FullName}'",
                        appDataDirectory.FullName);
                }

                exitCode = ExitCode.Success;
            }
        }
        else
        {
            if (_verboseLoggingEnabled)
            {
                logger.Verbose("Site has no App_Data directory: '{FullName}'", appDataDirectory.FullName);
            }

            exitCode = ExitCode.Success;
        }

        webJobStopwatch.Stop();
        if (_debugLoggingEnabled)
        {
            logger.Debug("Web jobs took {V} seconds",
                webJobStopwatch.Elapsed.TotalSeconds.ToString("F", CultureInfo.InvariantCulture));
        }

        return exitCode;
    }

    private async Task<ExitCode> CreateWebDeployPackagesAsync(
        FileEntry solutionFile,
        string configuration,
        ILogger logger,
        UPath platformDirectoryPath,
        WebSolutionProject solutionProject,
        string platformName)
    {
        if (solutionProject.NetFrameworkGeneration != NetFrameworkGeneration.NetFramework)
        {
            logger.Information("Skipping web deploy package, only .NET Framework projects supported");
            return ExitCode.Success;
        }

        if (_debugLoggingEnabled)
        {
            logger.Debug("Starting web deploy packaging");
        }

        var webDeployStopwatch = Stopwatch.StartNew();

        var webDeployPackageDirectoryPath = UPath.Combine(platformDirectoryPath, "WebDeploy");

        DirectoryEntry webDeployPackageDirectory =
            new DirectoryEntry(fileSystem, webDeployPackageDirectoryPath).EnsureExists();

        var packagePath = UPath.Combine(
            webDeployPackageDirectory.FullName,
            $"{solutionProject.ProjectName}_{configuration}.zip");

        var buildSitePackageArguments = new List<string>(20)
        {
            fileSystem.ConvertPathToInternal(solutionProject.FullPath.Path),
            _argHelper.FormatPropertyArg("configuration", configuration),
            _argHelper.FormatPropertyArg("platform", platformName),

// ReSharper disable once PossibleNullReferenceException
            _argHelper.FormatPropertyArg("SolutionDir", solutionFile.Directory.FullName),
            _argHelper.FormatPropertyArg("PackageLocation", fileSystem.ConvertPathToInternal(packagePath)),
            _argHelper.FormatArg("verbosity", _verbosity.Level),
            _argHelper.FormatArg("target", "Package"),
        };

        if (_assemblyVersionPatchingEnabled)
        {
            buildSitePackageArguments.Add(_argHelper.FormatPropertyArg("AssemblyVersion", _assemblyVersion));
            buildSitePackageArguments.Add(_argHelper.FormatPropertyArg("FileVersion", _assemblyFileVersion));
        }

        if (_processorCount.HasValue && _processorCount.Value >= 1)
        {
            buildSitePackageArguments.Add(
                $"/maxcpucount:{_processorCount.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (_showSummary)
        {
            buildSitePackageArguments.Add(_argHelper.FormatArg("detailedsummary"));
        }

        if (!_codeAnalysisEnabled)
        {
            buildSitePackageArguments.Add(_argHelper.FormatPropertyArg("RunCodeAnalysis", "false"));
        }

        var toolAction = _debugLoggingEnabled
            ? (message, _) => logger.Debug("{Message}", message)
            : (CategoryLog?)null;

        var exePath = AdjustBuildArgs(buildSitePackageArguments);

        var packageSiteExitCode =
            await
                ProcessRunner.ExecuteProcessAsync(
                    fileSystem.ConvertPathToInternal(exePath),
                    buildSitePackageArguments,
                    logger.Information,
                    logger.Error,
                    toolAction,
                    cancellationToken: _cancellationToken).ConfigureAwait(false);

        webDeployStopwatch.Stop();

        if (_debugLoggingEnabled)
        {
            logger.Debug("WebDeploy packaging took {TotalSeconds:F} seconds",
                webDeployStopwatch.Elapsed.TotalSeconds);
        }

        return packageSiteExitCode;
    }

    private IEnumerable<FileEntry> FindSolutionFiles(DirectoryEntry directoryEntry, ILogger logger)
    {
        var isExcludeListed = IsExcludeListed(directoryEntry);
        if (isExcludeListed.Item1)
        {
            if (_debugLoggingEnabled)
            {
                logger.Debug(
                    "Skipping directory '{FullName}' when searching for solution files because the directory is not allowed, {Item2}",
                    fileSystem.ConvertPathToInternal(directoryEntry.Path),
                    isExcludeListed.Item2);
            }

            return [];
        }

        var solutionFiles = directoryEntry.EnumerateFiles("*.sln").ToList();

        foreach (var subDir in directoryEntry.EnumerateDirectories())
        {
            solutionFiles.AddRange(FindSolutionFiles(subDir, logger));
        }

        return solutionFiles;
    }

    private (bool, string) IsExcludeListed(DirectoryEntry directoryEntry)
    {
        var isExcludedByName =
            _pathLookupSpecification.IsNotAllowed(directoryEntry, _vcsRoot);

        if (isExcludedByName.Item1)
        {
            return isExcludedByName;
        }

        FileAttributes[] excludedByAttributes = _excludeListedByAttributes.Where(
            excluded => (directoryEntry.Attributes & excluded) != 0).ToArray();

        bool isNotAllowedByAttributes = excludedByAttributes.Length > 0;

        return (isNotAllowedByAttributes, isNotAllowedByAttributes
            ? $"Directory has exclude-listed attributes {string.Join(", ", excludedByAttributes.Select(_ => Enum.GetName(typeof(FileAttributes), _)))}"
            : string.Empty);
    }

    private void RemoveXmlFilesForAssemblies(DirectoryEntry directoryEntry)
    {
        if (!directoryEntry.Exists)
        {
            return;
        }

        if (_verboseLoggingEnabled)
        {
            _logger.Verbose("Deleting XML files for corresponding DLL files in directory '{FullName}'",
                directoryEntry.FullName);
        }

        FileEntry[] dllFiles = directoryEntry.GetFiles("*.dll", SearchOption.AllDirectories);

        foreach (FileEntry dllFile in dllFiles)
        {
            var xmlFile = new FileEntry(fileSystem, UPath.Combine(
                dllFile.Directory.FullName!,
                $"{Path.GetFileNameWithoutExtension(dllFile.Name)}.xml"));

            if (xmlFile.Exists)
            {
                if (_verboseLoggingEnabled)
                {
                    _logger.Verbose("Deleting XML file '{FullName}'", xmlFile.FullName);
                }

                xmlFile.DeleteIfExists();

                if (_verboseLoggingEnabled)
                {
                    _logger.Verbose("Deleted XML file '{FullName}'", xmlFile.FullName);
                }
            }
        }
    }
}