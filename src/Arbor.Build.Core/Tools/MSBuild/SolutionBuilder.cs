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
using Arbor.KVConfiguration.Core.Metadata;
using Arbor.KVConfiguration.Schema.Json;
using Arbor.Processing;
using JetBrains.Annotations;

using Microsoft.Web.XmlTransform;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Arbor.Build.Core.Tools.MSBuild
{
    [Priority(300)]
    [UsedImplicitly]
    public class SolutionBuilder : ITool, IReportLogTail
    {
        private readonly List<FileAttributes> _blackListedByAttributes = new List<FileAttributes>
        {
            FileAttributes.Hidden,
            FileAttributes.System,
            FileAttributes.Offline,
            FileAttributes.Archive
        };

        private readonly BuildContext _buildContext;

        private readonly List<string> _knownPlatforms = new List<string> { "x86", "x64", "Any CPU" };

        private readonly PathLookupSpecification _pathLookupSpecification =
            DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "node_modules" });

        private readonly List<string> _platforms = new List<string>();

        private bool _appDataJobsEnabled;
        private bool _applicationMetadataDotNetConfigurationEnabled;
        private bool _applicationMetadataDotNetCpuPlatformEnabled;

        private bool _applicationMetadataEnabled;
        private bool _applicationMetadataGitBranchEnabled;
        private bool _applicationMetadataGitHashEnabled;

        private string _artifactsPath = null!;
        private string _assemblyFileVersion = null!;
        private string _assemblyVersion = null!;
        private string? _buildSuffix;

        private IReadOnlyCollection<IVariable> _buildVariables;
        private CancellationToken _cancellationToken;

        private bool _cleanBinXmlFilesForAssembliesEnabled;

        private bool _cleanWebJobsXmlFilesForAssembliesEnabled;

        private bool _codeAnalysisEnabled;
        private bool _configurationTransformsEnabled;

        private bool _createNuGetWebPackage;
        private bool _createWebDeployPackages;
        private bool _debugLoggingEnabled;
        private string? _defaultTarget;
        private string? _dotNetExePath;
        private bool _dotnetPackToolsEnabled;
        private bool _dotnetPublishEnabled = true;

        private IReadOnlyCollection<string> _excludedNuGetWebPackageFiles;
        private ImmutableArray<string> _excludedPlatforms;
        private IReadOnlyCollection<string> _excludedWebJobsDirectorySegments;

        private IReadOnlyCollection<string> _excludedWebJobsFiles;

        private IReadOnlyCollection<string> _filteredNuGetWebPackageProjects;

        private string? _gitHash;
        private ILogger _logger;
        private string _msBuildExe = null!;
        private string _packagesDirectory = null!;
        private bool _pdbArtifactsEnabled;
        private bool _preCompilationEnabled;
        private int? _processorCount = default;
        private string? _publishRuntimeIdentifier;

        private string? _ruleset;
        private bool _showSummary;
        private string _vcsRoot = null!;
        private bool _webProjectsBuildEnabled;
        private bool _verboseLoggingEnabled;
        private MSBuildVerbosityLevel _verbosity;
        private string _version = null!;
        private readonly NuGetPackager _nugetPackager;
        private bool _logMsBuildWarnings;
        private GitBranchModel? _gitModel;
        private BranchName? _branchName;
        private bool _deterministicBuildEnabled;

        public SolutionBuilder(BuildContext buildContext, NuGetPackager nugetPackager)
        {
            _buildContext = buildContext;
            _nugetPackager = nugetPackager;
            LogTail = new FixedSizedQueue<string>
            {
                Limit = 5
            };
        }

        private static bool BuildPlatformOrConfiguration(IReadOnlyCollection<IVariable> variables, string key)
        {
            bool enabled =
                variables.GetBooleanByKey(key, true);

            return enabled;
        }

        private string? FindRuleSet()
        {
            IReadOnlyCollection<FileInfo> fileInfos = new DirectoryInfo(_vcsRoot)
                .GetFilesRecursive(".ruleset".ValueToImmutableArray(), _pathLookupSpecification, _vcsRoot)
                .ToReadOnlyCollection();

            if (fileInfos.Count != 1)
            {
                if (fileInfos.Count == 0)
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
                            fileInfos.Count);
                    }
                }

                return null;
            }

            string foundRuleSet = fileInfos.Single().FullName;

            if (_verboseLoggingEnabled)
            {
                _logger.Verbose("Found one ruleset file '{FoundRuleSet}'", foundRuleSet);
            }

            return foundRuleSet;
        }

        private async Task<ExitCode> BuildAsync(ILogger logger, IReadOnlyCollection<IVariable> variables)
        {
            if (_buildContext.Configurations.Count == 0)
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

            Stopwatch findSolutionFiles = Stopwatch.StartNew();

            IReadOnlyCollection<FileInfo> solutionFiles =
                FindSolutionFiles(new DirectoryInfo(_vcsRoot), logger).ToReadOnlyCollection();

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

            IDictionary<FileInfo, IList<string>> solutionPlatforms =
                await GetSolutionPlatformsAsync(solutionFiles).ConfigureAwait(false);

            if (_verboseLoggingEnabled)
            {
                logger.Verbose("Found solutions and platforms: {NewLine}{V}",
                    Environment.NewLine,
                    string.Join(Environment.NewLine,
                        solutionPlatforms.Select(item => $"{item.Key}: [{string.Join(", ", item.Value)}]")));
            }

            foreach (KeyValuePair<FileInfo, IList<string>> solutionPlatform in solutionPlatforms)
            {
                string[] platforms = solutionPlatform.Value.ToArray();

                foreach (string platform in platforms)
                {
                    if (!_platforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
                    {
                        solutionPlatform.Value.Remove(platform);
                        logger.Debug("Removing found platform {Platform} found in file {SolutionFile}",
                            platform,
                            solutionPlatform.Key.FullName);
                    }
                }
            }

            KeyValuePair<FileInfo, IList<string>>[] filteredPlatforms = solutionPlatforms
                .Where(s => s.Value.Count > 0)
                .ToArray();

            if (filteredPlatforms.Length == 0)
            {
                logger.Error("Could not find any solution platforms");
                return ExitCode.Failure;
            }

            foreach (KeyValuePair<FileInfo, IList<string>> solutionPlatform in filteredPlatforms)
            {
                ExitCode result = await BuildSolutionForPlatformAsync(
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

        private async Task<IDictionary<FileInfo, IList<string>>> GetSolutionPlatformsAsync(
            IReadOnlyCollection<FileInfo> solutionFiles)
        {
            IDictionary<FileInfo, IList<string>> solutionPlatforms =
                new Dictionary<FileInfo, IList<string>>();

            foreach (FileInfo solutionFile in solutionFiles)
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

        private async Task<List<string>> GetSolutionPlatformsAsync(FileInfo solutionFile)
        {
            var platforms = new List<string>();

            await using (var fs = new FileStream(solutionFile.FullName, FileMode.Open, FileAccess.Read))
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

                    if (line.IndexOf(
                            "GlobalSection(SolutionConfigurationPlatforms)",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isInGlobalSection = true;
                        continue;
                    }

                    if (line.IndexOf(
                            "EndGlobalSection",
                            StringComparison.OrdinalIgnoreCase) >= 0)
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

            return platforms.Distinct().ToList();
        }

        private async Task<ExitCode> BuildSolutionForPlatformAsync(
            FileInfo solutionFile,
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
                    item => _buildContext.Configurations.Select(config => new { Platform = item, Configuration = config }))
                .ToList();

            if (combinations.Count > 1)
            {
                IEnumerable<Dictionary<string, string>> dictionaries =
                    combinations.Select(combination => new Dictionary<string, string>
                    {
                        { "Configuration", combination.Configuration },
                        { "Platform", combination.Platform }
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

            foreach (string configuration in _buildContext.Configurations)
            {
                _buildContext.CurrentBuildConfiguration = new BuildConfiguration(configuration);

                ExitCode result =
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
            FileInfo solutionFile,
            string configuration,
            ILogger logger,
            IEnumerable<string> platforms)
        {
            foreach (string knownPlatform in platforms)
            {
                Stopwatch buildStopwatch = Stopwatch.StartNew();
                if (_debugLoggingEnabled)
                {
                    logger.Debug("Starting stopwatch for solution file {Name} ({Configuration}|{KnownPlatform})",
                        solutionFile.Name,
                        configuration,
                        knownPlatform);
                }

                ExitCode result =
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
                        solutionFile.FullName,
                        configuration,
                        knownPlatform);
                    return result;
                }
            }

            return ExitCode.Success;
        }

        private async Task<ExitCode> BuildSolutionWithConfigurationAndPlatformAsync(
            FileInfo solutionFile,
            string configuration,
            string platform,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(_msBuildExe))
            {
                logger.Error("MSBuild path is not defined");
                return ExitCode.Failure;
            }

            if (!File.Exists(_msBuildExe))
            {
                logger.Error("The MSBuild path '{MsBuildExe}' does not exist", _msBuildExe);
                return ExitCode.Failure;
            }

            var argList = new List<string>(10)
            {
                solutionFile.FullName,
                $"/property:configuration={configuration}",
                $"/property:platform={platform}",
                $"/verbosity:{_verbosity.Level}",
                $"/target:{_defaultTarget}",
               $"/property:AssemblyVersion={_assemblyVersion}",
                $"/property:FileVersion={_assemblyFileVersion}",
                $"/property:Version={_version}"
            };

            if (_deterministicBuildEnabled)
            {
                argList.Add("/property:ContinuousIntegrationBuild=true");
            }

            if (_processorCount.HasValue && _processorCount.Value >= 1)
            {
                argList.Add($"/maxcpucount:{_processorCount.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (!_logMsBuildWarnings)
            {
                argList.Add("/clp:ErrorsOnly");
            }

            if (_codeAnalysisEnabled)
            {
                if (_verboseLoggingEnabled)
                {
                    logger.Verbose("Code analysis is enabled");
                }

                argList.Add("/property:RunCodeAnalysis=true");

                if (!string.IsNullOrWhiteSpace(_ruleset) && File.Exists(_ruleset))
                {
                    logger.Information("Using code analysis ruleset '{Ruleset}'", _ruleset);

                    argList.Add($"/property:CodeAnalysisRuleSet={_ruleset}");
                }
            }
            else
            {
                argList.Add("/property:RunCodeAnalysis=false");
                logger.Information("Code analysis is disabled");
            }

            if (_showSummary)
            {
                argList.Add("/detailedsummary");
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
                    argList.Select(arg => new Dictionary<string, string> { { "Value", arg } }).DisplayAsTable());
            }

            CategoryLog? verboseAction =
                _verboseLoggingEnabled ? logger.Verbose : (CategoryLog?) null;
            CategoryLog? debugAction = _verboseLoggingEnabled ? logger.Debug : (CategoryLog?)null;

            void LogDefault(string message, string category)
            {
                if (message.Trim().IndexOf("): warning ", StringComparison.Ordinal) >= 0)
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

            AdjustBuildArgs(argList);

            ExitCode exitCode =
                await
                    ProcessRunner.ExecuteProcessAsync(
                            _msBuildExe,
                            argList,
                            standardOutLog: LogDefault,
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

                    ExitCode webAppsExitCode =
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

                    ExitCode webAppsExitCode =
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

        private void AdjustBuildArgs(List<string> argList)
        {
            var fileInfo = new FileInfo(_msBuildExe);

            if (fileInfo.Name.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase)
                && argList.Count > 0
                && !argList[0].Equals("msbuild", StringComparison.OrdinalIgnoreCase))
            {
                argList.Insert(0, "msbuild");
            }
        }

        private async Task<ExitCode> PublishProjectsAsync(FileInfo solutionFile, string configuration, ILogger logger)
        {
            ExitCode exitCode = ExitCode.Success;

            if (_dotnetPublishEnabled)
            {
                _logger.Information("Dotnet publish is enabled, key {Key}",
                    WellKnownVariables.DotNetPublishExeProjectsEnabled);

                ExitCode webAppsExitCode =
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
            FileInfo solutionFile,
            string configuration,
            ILogger logger)
        {
            static bool HasPublishPackageEnabled(SolutionProject project)
            {
                return project.Project.HasPropertyWithValue("GeneratePackageOnBuild", "true");
            }

            if (string.IsNullOrWhiteSpace(_dotNetExePath))
            {
                logger.Warning("dotnet could not be found, skipping publishing dotnet exe projects");
                return ExitCode.Success;
            }

            Solution solution = Solution.LoadFrom(solutionFile.FullName);

            const string sdkTestPackageId = "Microsoft.NET.Test.Sdk";

            ImmutableArray<SolutionProject> publishProjects = solution.Projects
                .Where(project =>
                    project.NetFrameworkGeneration == NetFrameworkGeneration.NetCoreApp
                    && (
                        ((project.Project.HasPropertyWithValue("ArborPublishEnabled", "true") ||
                          !project.Project.PropertyGroups.Any(msBuildPropertyGroup =>
                              msBuildPropertyGroup.Properties.Any(msBuildProperty =>
                                  msBuildProperty.Name.Equals("ArborPublishEnabled", StringComparison.Ordinal))))
                         || (project.Project.HasPropertyWithValue("OutputType", "Exe")
                             || project.Project.Sdk == DotNetSdk.DotnetWeb
                             || HasPublishPackageEnabled(project)))
                        && !project.Project.PackageReferences.Any(reference =>
                            sdkTestPackageId.Equals(reference.Package, StringComparison.OrdinalIgnoreCase))))
                .ToImmutableArray();

            foreach (SolutionProject solutionProject in publishProjects)
            {
                if (solutionProject.Project.HasPropertyWithValue(WellKnownVariables.DotNetPublishExeEnabled, "false"))
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
                    "publish",
                    Path.GetFullPath(solutionProject.FullPath),
                    "-c",
                    configuration
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

                var packageLookupDirectories = new List<DirectoryInfo>();

                DirectoryInfo? tempDirectory = default;

                bool isReleaseBuild = configuration.Equals(WellKnownConfigurations.Release, StringComparison.OrdinalIgnoreCase);

                var options = GetVersionOptions(isReleaseBuild);

                string packageVersion = NuGetVersionHelper.GetPackageVersion(options);

                try
                {
                    if (HasPublishPackageEnabled(solutionProject))
                    {
                        args.Add($"/p:version={packageVersion}");
                        args.Add("--output");

                        string tempDirPath = Path.Combine(Path.GetTempPath(), "Arbor.Build-pkg" + DateTime.UtcNow.Ticks);
                        tempDirectory = new DirectoryInfo(tempDirPath);
                        tempDirectory.EnsureExists();

                        packageLookupDirectories.Add(new DirectoryInfo(solutionProject.ProjectDirectory));
                        packageLookupDirectories.Add(tempDirectory);

                        args.Add(tempDirectory.FullName);
                    }

                    if (!string.IsNullOrWhiteSpace(_publishRuntimeIdentifier))
                    {
                        args.Add("-r");
                        args.Add(_publishRuntimeIdentifier);
                    }

                    void Log(string message, string category)
                    {
                        if (message.Trim().IndexOf("): warning ", StringComparison.Ordinal) >= 0)
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

                    ExitCode projectExitCode = await ProcessRunner.ExecuteProcessAsync(
                                                   _dotNetExePath,
                                                   args,
                                                   standardOutLog: Log,
                                                   cancellationToken: _cancellationToken).ConfigureAwait(false);

                    if (!projectExitCode.IsSuccess)
                    {
                        return projectExitCode;
                    }
                }
                finally
                {
                    new DirectoryInfo(_packagesDirectory).EnsureExists();

                    foreach (var lookupDirectory in packageLookupDirectories)
                    {
                        if (lookupDirectory != null)
                        {
                            lookupDirectory.Refresh();

                            if (lookupDirectory.Exists)
                            {
                                var nugetPackages = lookupDirectory.GetFiles($"*{packageVersion}.nupkg", SearchOption.AllDirectories);

                                foreach (var nugetPackage in nugetPackages)
                                {
                                    string targetFile = Path.Combine(_packagesDirectory, nugetPackage.Name);

                                    if (!File.Exists(targetFile))
                                    {
                                        nugetPackage.CopyTo(targetFile, true);
                                    }
                                }

                                var nugetSymbolPackages = lookupDirectory.GetFiles($"*{packageVersion}.snupkg", SearchOption.AllDirectories);

                                foreach (var nugetPackage in nugetSymbolPackages)
                                {
                                    string targetFile = Path.Combine(_packagesDirectory, nugetPackage.Name);

                                    if (!File.Exists(targetFile))
                                    {
                                        nugetPackage.CopyTo(targetFile, true);
                                    }
                                }
                            }
                        }
                    }

                    try
                    {
                        tempDirectory.DeleteIfExists(true);
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
            FileInfo solutionFile,
            string configuration,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(_dotNetExePath))
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

            Solution solution = Solution.LoadFrom(solutionFile.FullName);

            ImmutableArray<SolutionProject> exeProjects = solution.Projects.Where(IsPackageProject).ToImmutableArray();

            bool isReleaseBuild = configuration.Equals(WellKnownConfigurations.Release, StringComparison.OrdinalIgnoreCase);

            var options = GetVersionOptions(isReleaseBuild);
            string packageVersion = NuGetVersionHelper.GetPackageVersion(options);

            foreach (SolutionProject solutionProject in exeProjects)
            {
                EnsureFileDates(new DirectoryInfo(solutionProject.ProjectDirectory));

                string[] args =
                {
                    "pack",
                    solutionProject.FullPath,
                    "--configuration",
                    configuration,
                    $"/p:VersionPrefix={packageVersion}",
                    "--output",
                    _packagesDirectory,
                    "--no-build",
                    "--include-symbols",
                };

                void Log(string message, string category)
                {
                    if (message.Trim().IndexOf("): warning ", StringComparison.Ordinal) >= 0)
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

                ExitCode projectExitCode = await ProcessRunner.ExecuteProcessAsync(_dotNetExePath,
                    args,
                    standardOutLog: Log,
                    cancellationToken: _cancellationToken).ConfigureAwait(false);

                if (!projectExitCode.IsSuccess)
                {
                    return projectExitCode;
                }
            }

            return ExitCode.Success;
        }

        private void EnsureFileDates(DirectoryInfo directoryInfo)
        {
            if (directoryInfo is null)
            {
                return;
            }

            var files = directoryInfo.GetFiles();

            foreach (var fileInfo in files)
            {
                try
                {
                    fileInfo.FullName.EnsureHasValidDate(_logger);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Could not ensure dates for file '{File}'", fileInfo.FullName);
                }
            }

            foreach (var subDirectory in directoryInfo.GetDirectories())
            {
                EnsureFileDates(subDirectory);
            }
        }

        private void CopyCodeAnalysisReportsToArtifacts(string configuration, string platform, ILogger logger)
        {
            IReadOnlyCollection<FileInfo> analysisLogFiles =
                new DirectoryInfo(_vcsRoot).GetFiles("*.AnalysisLog.xml", SearchOption.AllDirectories)
                    .ToReadOnlyCollection();

            DirectoryInfo targetReportDirectory =
                new DirectoryInfo(Path.Combine(_artifactsPath, "CodeAnalysis")).EnsureExists();

            if (_verboseLoggingEnabled)
            {
                logger.Verbose("Found {Count} code analysis log files: {V}",
                    analysisLogFiles.Count,
                    string.Join(Environment.NewLine, analysisLogFiles.Select(file => file.FullName)));
            }

            foreach (FileInfo analysisLogFile in analysisLogFiles)
            {
                string projectName = analysisLogFile.Name.Replace(".CodeAnalysisLog.xml", string.Empty, StringComparison.OrdinalIgnoreCase);

                string targetFilePath = Path.Combine(
                    targetReportDirectory.FullName,
                    $"{projectName}.{Platforms.Normalize(platform)}.{configuration}.xml");

                analysisLogFile.CopyTo(targetFilePath);
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

                var sourceRootDirectory = new DirectoryInfo(_vcsRoot);

                IReadOnlyCollection<FileInfo> files = sourceRootDirectory.GetFilesRecursive(
                        new[] { ".pdb", ".dll" },
                        pathLookupSpecification,
                        _vcsRoot)
                    .OrderBy(file => file.FullName)
                    .ToReadOnlyCollection();

                IReadOnlyCollection<FileInfo> pdbFiles =
                    files.Where(file => file.Extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase))
                        .ToReadOnlyCollection();

                IReadOnlyCollection<FileInfo> dllFiles =
                    files.Where(file => file.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
                        .ToReadOnlyCollection();
                if (_debugLoggingEnabled)
                {
                    _logger.Debug("Found files as PDB artifacts {V}",
                        string.Join(Environment.NewLine, pdbFiles.Select(file => "\tPDB: " + file.FullName)));
                }

                var pairs = pdbFiles
                    .Select(pdb => new
                    {
                        PdbFile = pdb,
                        DllFile = dllFiles
                            .SingleOrDefault(dll => dll.FullName
                                .Equals(
                                    Path.Combine(
                                        pdb.Directory?.FullName,
                                        $"{Path.GetFileNameWithoutExtension(pdb.Name)}.dll"),
                                    StringComparison.OrdinalIgnoreCase))
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
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("Copying PDB file '{FullName}' to '{TargetFilePath}'",
                                pair.PdbFile.FullName,
                                targetFilePath);
                        }

                        pair.PdbFile.CopyTo(targetFilePath);
                    }
                    else
                    {
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("Target file '{TargetFilePath}' already exists, skipping file",
                                targetFilePath);
                        }
                    }

                    if (pair.DllFile != null)
                    {
                        string targetDllFilePath = Path.Combine(targetDirectory.FullName, pair.DllFile.Name);

                        if (!File.Exists(targetDllFilePath))
                        {
                            if (_debugLoggingEnabled)
                            {
                                _logger.Debug("Copying DLL file '{FullName}' to '{TargetFilePath}'",
                                    pair.DllFile.FullName,
                                    targetFilePath);
                            }

                            pair.DllFile.CopyTo(targetDllFilePath);
                        }
                        else
                        {
                            if (_debugLoggingEnabled)
                            {
                                _logger.Debug("Target DLL file '{TargetDllFilePath}' already exists, skipping file",
                                    targetDllFilePath);
                            }
                        }
                    }
                    else
                    {
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("DLL file for PDB '{FullName}' was not found", pair.PdbFile.FullName);
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
            FileInfo solutionFile,
            string configuration,
            string platform,
            ILogger logger)
        {
            Solution solution = Solution.LoadFrom(solutionFile.FullName);

            List<SolutionProject> webProjects =
                solution.Projects.Where(
                        project => project.Project.ProjectTypes.Any(type => type == ProjectType.Mvc5))
                    .ToList();
            if (_debugLoggingEnabled)
            {
                logger.Debug("Finding WebApplications by looking at project type GUID {WebApplicationProjectTypeId}",
                    ProjectType.Mvc5);
            }

            logger.Information("WebApplication projects to build [{Count}]: {Projects}",
                webProjects.Count,
                string.Join(", ", webProjects.Select(wp => wp.Project.FileName)));

            var webSolutionProjects = new List<WebSolutionProject>();

            webSolutionProjects.AddRange(webProjects.Select(project => new WebSolutionProject(
                project.Project.FileName,
                project.Project.ProjectName,
                project.Project.ProjectDirectory,
                project.Project,
                NetFrameworkGeneration.NetFramework)));

            ImmutableArray<WebSolutionProject> solutionProjects = solution.Projects
                .Where(project => File.ReadAllLines(project.Project.FileName)
                                      .First()
                                      .IndexOf("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) >= 0)
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

                    ExitCode exitCode = await CopyKuduWebJobsAsync(logger, solutionProject, siteArtifactDirectory)
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

                    ExitCode packageSiteExitCode =
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

                    ExitCode packageSiteExitCode = await CreateNuGetWebPackagesAsync(
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

        private Task CreateApplicationMetadataAsync(
            DirectoryInfo siteArtifactDirectory,
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

            string applicationMetadataJsonFilePath;

            const string applicationmetadataJson = "applicationmetadata.json";

            if (Directory.Exists(Path.Combine(siteArtifactDirectory.FullName, "wwwroot")))
            {
                applicationMetadataJsonFilePath = Path.Combine(
                    siteArtifactDirectory.FullName,
                    "wwwroot",
                    applicationmetadataJson);
            }
            else
            {
                applicationMetadataJsonFilePath = Path.Combine(
                    siteArtifactDirectory.FullName,
                    applicationmetadataJson);
            }

            File.WriteAllText(applicationMetadataJsonFilePath, serialize, Encoding.UTF8);

            string keyPluralSingular = items.Count == 1 ? "key" : "keys";
            string verb = items.Count == 1 ? "has" : "have";

            _logger.Information(
                "{Count} metadata {KeyPluralSingular} {Verb} been written to '{ApplicationMetadataJsonFilePath}'",
                items.Count,
                keyPluralSingular,
                verb,
                applicationMetadataJsonFilePath);

            return Task.CompletedTask;
        }

        private async Task<ExitCode> BuildWebApplicationAsync(
            FileInfo solutionFile,
            string configuration,
            ILogger logger,
            WebSolutionProject solutionProject,
            string platformName,
            DirectoryInfo siteArtifactDirectory)
        {
            List<string> buildSiteArguments;

            string target;

            if (solutionProject.NetFrameworkGeneration == NetFrameworkGeneration.NetFramework)
            {
                target = "pipelinePreDeployCopyAllFilesToOneFolder";

                buildSiteArguments = new List<string>(15)
                {
                    solutionProject.FullPath,
                    $"/property:configuration={configuration}",
                    $"/property:platform={platformName}",
                    $"/property:_PackageTempDir={siteArtifactDirectory.FullName}",

                    // ReSharper disable once PossibleNullReferenceException
                    $"/property:SolutionDir={solutionFile.Directory.FullName}",
                    $"/verbosity:{_verbosity.Level}",
                    "/property:AutoParameterizationWebConfigConnectionStrings=false"

                };

                if (_preCompilationEnabled)
                {
                    _logger.Information("Pre-compilation is enabled");
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
                    $"/property:publishdir={siteArtifactDirectory.FullName}",
                    $"/property:AssemblyVersion={_assemblyVersion}",
                    $"/property:FileVersion={_assemblyFileVersion}"
                };

                if (_deterministicBuildEnabled)
                {
                    buildSiteArguments.Add("/property:ContinuousIntegrationBuild=true");
                }

                string rid =
                    solutionProject.Project.GetPropertyValue(WellKnownVariables.ProjectMSBuildPublishRuntimeIdentifier);

                if (!string.IsNullOrWhiteSpace(rid))
                {
                    buildSiteArguments.Add($"/property:RuntimeIdentifier={rid}");
                }

                target = "restore;rebuild;publish";
            }

            if (_processorCount.HasValue && _processorCount.Value >= 1)
            {
                buildSiteArguments.Add($"/maxcpucount:{_processorCount.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (_showSummary)
            {
                buildSiteArguments.Add("/detailedsummary");
            }

            buildSiteArguments.Add($"/target:{target}");

            if (!_codeAnalysisEnabled)
            {
                buildSiteArguments.Add("/property:RunCodeAnalysis=false");
            }

            ExitCode buildSiteExitCode =
                await
                    ProcessRunner.ExecuteProcessAsync(
                        _msBuildExe,
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

                    var binDirectory = new DirectoryInfo(Path.Combine(siteArtifactDirectory.FullName, "bin"));

                    if (binDirectory.Exists)
                    {
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("The bin directory '{FullName}' does exist", binDirectory.FullName);
                        }

                        RemoveXmlFilesForAssemblies(binDirectory);
                    }
                    else
                    {
                        if (_debugLoggingEnabled)
                        {
                            _logger.Debug("The bin directory '{FullName}' does not exist", binDirectory.FullName);
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
            string platformDirectoryPath,
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

            List<MSBuildProperty> msbuildProperties =
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

            var artifactDirectory = new DirectoryInfo(siteArtifactDirectory);

            ImmutableArray<string> allIncludedFiles =
                artifactDirectory.GetFilesWithWithExclusions(_excludedNuGetWebPackageFiles);

            ExitCode exitCode = await CreateNuGetPackageAsync(
                platformDirectoryPath,
                logger,
                packageId,
                allIncludedFiles,
                "",
                artifactDirectory).ConfigureAwait(false);

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

            var environmentFiles = new DirectoryInfo(solutionProject.ProjectDirectory)
                .GetFilesRecursive(rootDir: _vcsRoot)
                .Select(file => new { File = file, Parts = file.Name.Split(separator) })
                .Where(item => item.Parts.Length == fileNameMinPartCount
                               && item.Parts[1].Equals(environmentLiteral, StringComparison.OrdinalIgnoreCase))
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

                List<string> elements = environmentFiles
                    .Where(file => file.EnvironmentName.Equals(environmentName, StringComparison.OrdinalIgnoreCase))
                    .Select(file => file.File.FullName)
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

                ExitCode environmentPackageExitCode = await CreateNuGetPackageAsync(
                    platformDirectoryPath,
                    logger,
                    environmentPackageId,
                    elements,
                    $".Environment.{environmentName}",
                    new DirectoryInfo(rootDirectory)).ConfigureAwait(false);

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

                string normalizedProjectFileName = Path.GetFileNameWithoutExtension(solutionProject.FullPath);

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
                        solutionProject.FullPath,
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
            string platformDirectoryPath,
            ILogger logger,
            string packageId,
            IReadOnlyCollection<string> filesList,
            string packageNameSuffix,
            DirectoryInfo baseDirectory)
        {
            string packageDirectoryPath = Path.Combine(platformDirectoryPath, "NuGet");

            DirectoryInfo packageDirectory = new DirectoryInfo(packageDirectoryPath).EnsureExists();

            NuGetPackageConfiguration? packageConfiguration = _nugetPackager.GetNuGetPackageConfiguration(
                logger,
                _buildVariables,
                packageDirectory.FullName,
                _vcsRoot,
                packageNameSuffix);

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
                filesList.Select(file => NuSpecHelper.IncludedFile(file, baseDirectory.FullName, _logger)));

            FileListWithChecksumFile contentFilesInfo = ChecksumHelper.CreateFileListForDirectory(baseDirectory);

            string metaDir = new FileInfo(contentFilesInfo.ContentFilesFile).Directory?.FullName ??
                             throw new InvalidOperationException(Resources.ContentFullPathIsNull);

            string contentFileListFile =
                $@"<file src=""{contentFilesInfo.ContentFilesFile}"" target=""{contentFilesInfo.ContentFilesFile.Substring(metaDir.Length).TrimStart(Path.DirectorySeparatorChar)}"" />";
            string checksumFile =
                $@"<file src=""{contentFilesInfo.ChecksumFile}"" target=""{contentFilesInfo.ChecksumFile.Substring(metaDir.Length).TrimStart(Path.DirectorySeparatorChar)}"" />";

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

            DirectoryInfo tempDir = new DirectoryInfo(Path.Combine(
                    Path.GetTempPath(),
                    $"{DefaultPaths.TempPathPrefix}_sb_{DateTime.Now.Ticks}"))
                .EnsureExists();

            string nuspecTempFile = Path.Combine(tempDir.FullName, $"{packageId}.nuspec");

            await File
                .WriteAllTextAsync(nuspecTempFile, nuspecContent, Encoding.UTF8, _cancellationToken)
                .ConfigureAwait(false);

            ExitCode exitCode = await _nugetPackager.CreatePackageAsync(
                nuspecTempFile,
                packageConfiguration,
                true,
                _cancellationToken).ConfigureAwait(false);

            if (Directory.Exists(metaDir))
            {
                new DirectoryInfo(metaDir).DeleteIfExists();
            }

            File.Delete(nuspecTempFile);

            tempDir.DeleteIfExists();

            return exitCode;
        }

        private void TransformFiles(
            string configuration,
            ILogger logger,
            WebSolutionProject solutionProject,
            DirectoryInfo siteArtifactDirectory)
        {
            if (_debugLoggingEnabled)
            {
                logger.Debug("Transforms are enabled");

                logger.Debug("Starting xml transformations");
            }

            Stopwatch transformationStopwatch = Stopwatch.StartNew();
            string projectDirectoryPath = solutionProject.ProjectDirectory;

            string[] extensions = { ".xml", ".config" };

            IReadOnlyCollection<FileInfo> files = new DirectoryInfo(projectDirectoryPath)
                .GetFilesRecursive(extensions)
                .Where(
                    file =>
                        !_pathLookupSpecification.IsNotAllowed(file.DirectoryName).Item1
                        && !_pathLookupSpecification.IsFileExcluded(file.FullName, _vcsRoot).Item1)
                .Where(
                    file =>
                        extensions.Any(
                            extension =>
                                Path.GetExtension(file.Name)
                                    .Equals(extension, StringComparison.OrdinalIgnoreCase))
                        && !file.Name.Equals("web.config", StringComparison.OrdinalIgnoreCase))
                .ToImmutableArray();

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
            if (_debugLoggingEnabled)
            {
                logger.Debug("Found {Length} files with transforms", transformationPairs.Length);
            }

            foreach (var configurationFile in transformationPairs)
            {
                string relativeFilePath =
                    configurationFile.Original.FullName.Replace(projectDirectoryPath,
                        string.Empty,
                        StringComparison.InvariantCulture);

                string targetTransformResultPath = $"{siteArtifactDirectory.FullName}{relativeFilePath}";

                using var transformable = new XmlTransformableDocument();
                transformable.Load(configurationFile.Original.FullName);

                using var transformation = new XmlTransformation(configurationFile.TransformFile);
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

        [NotNull]
        private async Task<ExitCode> CopyKuduWebJobsAsync(
            [NotNull] ILogger logger,
            [NotNull] WebSolutionProject solutionProject,
            [NotNull] DirectoryInfo siteArtifactDirectory)
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

            Stopwatch webJobStopwatch = Stopwatch.StartNew();

            ExitCode exitCode;

            string appDataPath = Path.Combine(solutionProject.ProjectDirectory, "App_Data");

            var appDataDirectory = new DirectoryInfo(appDataPath);

            if (appDataDirectory.Exists)
            {
                if (_verboseLoggingEnabled)
                {
                    logger.Verbose("Site has App_Data directory: '{FullName}'", appDataDirectory.FullName);
                }

                DirectoryInfo kuduWebJobs =
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

                    string artifactJobAppDataPath = Path.Combine(
                        siteArtifactDirectory.FullName,
                        "App_Data",
                        "jobs");

                    DirectoryInfo artifactJobAppDataDirectory =
                        new DirectoryInfo(artifactJobAppDataPath).EnsureExists();

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
                                    kuduWebJobs.FullName,
                                    artifactJobAppDataDirectory.FullName,
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

                            var binDirectory = new DirectoryInfo(Path.Combine(artifactJobAppDataDirectory.FullName));

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
            FileInfo solutionFile,
            string configuration,
            ILogger logger,
            string platformDirectoryPath,
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

            Stopwatch webDeployStopwatch = Stopwatch.StartNew();

            string webDeployPackageDirectoryPath = Path.Combine(platformDirectoryPath, "WebDeploy");

            DirectoryInfo webDeployPackageDirectory = new DirectoryInfo(webDeployPackageDirectoryPath).EnsureExists();

            string packagePath = Path.Combine(
                webDeployPackageDirectory.FullName,
                $"{solutionProject.ProjectName}_{configuration}.zip");

            var buildSitePackageArguments = new List<string>(20)
            {
                solutionProject.FullPath,
                $"/property:configuration={configuration}",
                $"/property:platform={platformName}",

// ReSharper disable once PossibleNullReferenceException
                $"/property:SolutionDir={solutionFile.Directory.FullName}",
                $"/property:PackageLocation={packagePath}",
                $"/verbosity:{_verbosity.Level}",
                "/target:Package",
                $"/property:AssemblyVersion={_assemblyVersion}",
                $"/property:FileVersion={_assemblyFileVersion}"
            };

            if (_processorCount.HasValue && _processorCount.Value >= 1)
            {
                buildSitePackageArguments.Add($"/maxcpucount:{_processorCount.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (_showSummary)
            {
                buildSitePackageArguments.Add("/detailedsummary");
            }

            if (!_codeAnalysisEnabled)
            {
                buildSitePackageArguments.Add("/property:RunCodeAnalysis=false");
            }

            CategoryLog? toolAction = _debugLoggingEnabled
                ? (message, _) => logger.Debug("{Message}", message)
                : (CategoryLog?)null;

            ExitCode packageSiteExitCode =
                await
                    ProcessRunner.ExecuteProcessAsync(
                        _msBuildExe,
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

        private IEnumerable<FileInfo> FindSolutionFiles(DirectoryInfo directoryInfo, ILogger logger)
        {
            (bool, string) isBlacklisted = IsBlacklisted(directoryInfo);
            if (isBlacklisted.Item1)
            {
                if (_debugLoggingEnabled)
                {
                    logger.Debug(
                        "Skipping directory '{FullName}' when searching for solution files because the directory is notallowed, {Item2}",
                        directoryInfo.FullName,
                        isBlacklisted.Item2);
                }

                return Enumerable.Empty<FileInfo>();
            }

            List<FileInfo> solutionFiles = directoryInfo.EnumerateFiles("*.sln").ToList();

            foreach (DirectoryInfo subDir in directoryInfo.EnumerateDirectories())
            {
                solutionFiles.AddRange(FindSolutionFiles(subDir, logger));
            }

            return solutionFiles;
        }

        private (bool, string) IsBlacklisted(DirectoryInfo directoryInfo)
        {
            (bool, string) isBlacklistedByName =
                _pathLookupSpecification.IsNotAllowed(directoryInfo.FullName, _vcsRoot);

            if (isBlacklistedByName.Item1)
            {
                return isBlacklistedByName;
            }

            FileAttributes[] blackListedByAttributes = _blackListedByAttributes.Where(
                blackListed => (directoryInfo.Attributes & blackListed) != 0).ToArray();

            bool isNotAllowedByAttributes = blackListedByAttributes.Length > 0;

            return (isNotAllowedByAttributes, isNotAllowedByAttributes
                ? $"Directory has black-listed attributes {string.Join(", ", blackListedByAttributes.Select(_ => Enum.GetName(typeof(FileAttributes), _)))}"
                : string.Empty);
        }

        private void RemoveXmlFilesForAssemblies(DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.Exists)
            {
                return;
            }

            if (_verboseLoggingEnabled)
            {
                _logger.Verbose("Deleting XML files for corresponding DLL files in directory '{FullName}'",
                    directoryInfo.FullName);
            }

            FileInfo[] dllFiles = directoryInfo.GetFiles("*.dll", SearchOption.AllDirectories);

            foreach (FileInfo fileInfo in dllFiles)
            {
                var xmlFile = new FileInfo(Path.Combine(
                    fileInfo.Directory?.FullName!,
                    $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}.xml"));

                if (xmlFile.Exists)
                {
                    if (_verboseLoggingEnabled)
                    {
                        _logger.Verbose("Deleting XML file '{FullName}'", xmlFile.FullName);
                    }

                    File.Delete(xmlFile.FullName);

                    if (_verboseLoggingEnabled)
                    {
                        _logger.Verbose("Deleted XML file '{FullName}'", xmlFile.FullName);
                    }
                }
            }
        }

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            _buildVariables = buildVariables;
            _logger = logger ?? Logger.None??throw new ArgumentNullException(nameof(logger));
            _debugLoggingEnabled = _logger.IsEnabled(LogEventLevel.Debug);
            _verboseLoggingEnabled = _logger.IsEnabled(LogEventLevel.Verbose);
            _cancellationToken = cancellationToken;
            _msBuildExe =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSBuild_ExePath).GetValueOrThrow();
            _artifactsPath =
                buildVariables.Require(WellKnownVariables.Artifacts).GetValueOrThrow();

            _appDataJobsEnabled = buildVariables.GetBooleanByKey(
                WellKnownVariables.AppDataJobsEnabled);

            _buildSuffix =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.NuGetPackageArtifactsSuffix, null);

            if (!buildVariables.GetBooleanByKey(WellKnownVariables.NuGetPackageArtifactsSuffixEnabled, true))
            {
                _buildSuffix = "";
            }

            _version = buildVariables.Require(WellKnownVariables.Version).GetValueOrThrow();

            IVariable artifacts = buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue();
            _packagesDirectory = Path.Combine(artifacts.Value, "packages");

            _dotnetPackToolsEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.DotNetPackToolProjectsEnabled, true);

            _dotnetPublishEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.DotNetPublishExeProjectsEnabled, true);

            _webProjectsBuildEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.WebProjectsBuildEnabled, true);

            _dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty);

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

            _vcsRoot = buildVariables.Require(WellKnownVariables.SourceRoot).GetValueOrThrow();

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

            string? gitModel = buildVariables.GetVariableValueOrDefault(WellKnownVariables.GitBranchModel, null);

            if (GitBranchModel.TryParse(gitModel, out var model))
            {
                _gitModel = model;
            }

            _deterministicBuildEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.DeterministicBuildEnabled);

            var maybe = BranchName.TryParse(buildVariables.GetVariableValueOrDefault(WellKnownVariables.BranchName, null));

            _branchName = maybe;

            if (_vcsRoot is null)
            {
                _logger.Error("Could not find version control root path");
                return ExitCode.Failure;
            }

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

        public FixedSizedQueue<string> LogTail { get; }
    }
}
