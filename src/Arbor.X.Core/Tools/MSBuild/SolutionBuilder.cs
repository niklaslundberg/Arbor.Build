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

        readonly List<string> _knownConfiguration = new List<string> {"debug", "release"};
        readonly List<string> _knownPlatforms = new List<string> {"x86", "Any CPU"};
        string _artifactsPath;
        string _branchName;
        CancellationToken _cancellationToken;
        string _msBuildExe;
        int _processorCount;
        string _verbosity;

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _msBuildExe =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSBuild_ExePath).ThrowIfEmptyValue().Value;
            _artifactsPath =
                buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;
            _branchName =
                buildVariables.Require(WellKnownVariables.BranchName).ThrowIfEmptyValue().Value;
            
            var maxProcessorCount = ProcessorCount(buildVariables);

            int cpus = 1;

            if (buildVariables.HasKey(WellKnownVariables.CpuLimit))
            {
                int maxCpuLimit;
                if (int.TryParse(buildVariables.GetVariable(WellKnownVariables.CpuLimit).Value, out maxCpuLimit) &&
                    maxCpuLimit > 0)
                {
                    if (maxCpuLimit <= maxProcessorCount)
                    {
                        logger.Write(string.Format("Using CPU limit: {0}", maxCpuLimit));
                        cpus = maxCpuLimit;
                    }
                    else
                    {
                        logger.WriteWarning(string.Format("Invalid CPU limit: {0}", maxCpuLimit));
                    }
                }
            }
            else
            {
                cpus = maxProcessorCount;
            }

            _processorCount = cpus;

            _verbosity = "normal";

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

            if (buildVariables.HasKey(WellKnownVariables.ExternalTools_Kudu_ProcessorCount))
            {
                var value = buildVariables.Require(WellKnownVariables.ExternalTools_Kudu_ProcessorCount).Value;

                int parsedCount;

                if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out parsedCount) && parsedCount >= 1)
                {
                    processorCount = parsedCount;
                }
            }
            return processorCount;
        }

        async Task<ExitCode> BuildAsync(ILogger logger, IReadOnlyCollection<IVariable> variables)
        {
            var vcsRoot = VcsPathHelper.TryFindVcsRootPath();

            if (vcsRoot == null)
            {
                logger.WriteError("Could not find version control root path");
                return ExitCode.Failure;
            }

            bool buildAnyCpu = BuildPlatformOrConfiguration(variables, WellKnownVariables.IgnoreAnyCpu);

            if (!buildAnyCpu)
            {
                _knownPlatforms.Remove("Any CPU");
                logger.Write(string.Format("Flag {0} is set, ignoring AnyCPU builds", WellKnownVariables.IgnoreAnyCpu));
            }

            bool buildRelease = BuildPlatformOrConfiguration(variables, WellKnownVariables.IgnoreRelease);

            if (!buildRelease)
            {
                _knownConfiguration.Remove("release");
                logger.Write(string.Format("Flag {0} is set, ignoring release builds", WellKnownVariables.IgnoreRelease));
            }

            bool buildDebug = BuildPlatformOrConfiguration(variables, WellKnownVariables.IgnoreDebug);

            if (!buildDebug)
            {
                _knownConfiguration.Remove("debug");
                logger.Write(string.Format("Flag {0} is set, ignoring debug builds", WellKnownVariables.IgnoreDebug));
            }


            IEnumerable<FileInfo> solutionFiles = FindSolutionFiles(new DirectoryInfo(vcsRoot));

            IDictionary<FileInfo, IReadOnlyList<string>> solutionPlatforms =
                new Dictionary<FileInfo, IReadOnlyList<string>>();

            foreach (var solutionFile in solutionFiles)
            {
                var platforms = await GetSolutionPlatformsAsync(solutionFile);

                solutionPlatforms.Add(solutionFile, platforms);
            }

            logger.Write(string.Format("Found solutions and platforms: {0}{1}",
                Environment.NewLine,
                string.Join(Environment.NewLine,
                    solutionPlatforms.Select(
                        item => string.Format("{0}: [{1}]", item.Key, string.Join(", ", item.Value))))));

            foreach (var solutionPlatform in solutionPlatforms)
            {
                var result = await BuildSolutionAsync(solutionPlatform.Key, solutionPlatform.Value, logger);

                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            return ExitCode.Success;
        }

        bool BuildPlatformOrConfiguration(IEnumerable<IVariable> variables, string key)
        {
            var ignoreVariable =
                variables.SingleOrDefault(@var => @var.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));

            if (ignoreVariable != null)
            {
                if (!string.IsNullOrWhiteSpace(ignoreVariable.Value))
                {
                    bool ignore;
                    if (bool.TryParse(ignoreVariable.Value, out ignore) && ignore)
                    {
                        return false;
                    }
                }
            }
            return true;
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
                        var line = await streamReader.ReadLineAsync();

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
                            platforms.AddRange(_knownPlatforms.Where(knownPlatform =>
                                line.IndexOf(knownPlatform, StringComparison.InvariantCulture) >= 0));
                        }
                    }
                }
            }

            return platforms.Distinct().ToList();
        }

        async Task<ExitCode> BuildSolutionAsync(FileInfo solutionFile, IReadOnlyList<string> platforms, ILogger logger)
        {
            foreach (var configuration in _knownConfiguration)
            {
                var result = await BuildSolutionWithConfigurationAsync(solutionFile, configuration, logger, platforms);

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
            foreach (var knownPlatform in platforms)
            {
                var result =
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
                              string.Format("/verbosity:{0}", _verbosity),
                              "/target:rebuild",
                              "/detailedsummary",
                              string.Format("/maxcpucount:{0}", _processorCount.ToString(CultureInfo.InvariantCulture))
                          };

            var exitCode =
                await ProcessRunner.ExecuteAsync(_msBuildExe, arguments: argList, standardOutLog: logger.Write,
                    standardErrorAction: logger.WriteError, toolAction: logger.Write,
                    cancellationToken: _cancellationToken);

            if (exitCode.IsSuccess)
            {
                var webAppsExiteCode = await BuildWebApplicationsAsync(solutionFile, configuration, platform, logger);

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
            var solution = Solution.LoadFrom(solutionFile.FullName);

            var webProjects =
                solution.Projects.Where(
                    project => project.Project.ProjectTypes().Any(type => type == WebApplicationProjectTypeId)).ToList();

            logger.Write(string.Format("WebApplication projects to build [{0}]: {1}", webProjects.Count,
                string.Join(", ", webProjects.Select(wp => wp.Project.FileName))));

            foreach (var solutionProject in webProjects)
            {
                var siteArtifactDirectory =
                    new DirectoryInfo(Path.Combine(_artifactsPath, "Websites", solutionProject.ProjectName, platform,
                        configuration)).EnsureExists();

                var platformName = platform == "Any CPU" ? "AnyCPU" : platform;

                var argList = new List<string>
                              {
                                  solutionProject.Project.FileName,
                                  string.Format("/property:configuration={0}", configuration),
                                  string.Format("/property:platform={0}", platformName),
                                  string.Format("/property:_PackageTempDir={0}", siteArtifactDirectory),
                                  string.Format("/property:SolutionDir={0}", solutionFile.Directory.FullName),
                                  string.Format("/verbosity:{0}", _verbosity),
                                  "/target:pipelinePreDeployCopyAllFilesToOneFolder",
                                  "/property:AutoParameterizationWebConfigConnectionStrings=false",
                                  "/detailedsummary",
                                  string.Format("/maxcpucount:{0}",
                                      _processorCount.ToString(CultureInfo.InvariantCulture))
                              };

                var exitCode =
                    await ProcessRunner.ExecuteAsync(_msBuildExe, arguments: argList, standardOutLog: logger.Write,
                        standardErrorAction: logger.WriteError, toolAction: logger.Write,
                        cancellationToken: _cancellationToken);

                if (!exitCode.IsSuccess)
                {
                    return exitCode;
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

            var solutionFiles = directoryInfo.EnumerateFiles("*.sln").ToList();

            foreach (var subDir in directoryInfo.EnumerateDirectories())
            {
                solutionFiles.AddRange(FindSolutionFiles(subDir));
            }

            return solutionFiles;
        }

        bool IsBlacklisted(DirectoryInfo directoryInfo)
        {
            var isBlacklistedByFullName = _blackListedByName.Any(
                blackListed => directoryInfo.Name.Equals(blackListed, StringComparison.InvariantCultureIgnoreCase));

            var blackListedByStartName = _blackListedByStartName.Any(
                blackListed => directoryInfo.Name.StartsWith(blackListed, StringComparison.InvariantCultureIgnoreCase));

            var isBlackListedByAttributes = _blackListedByAttributes.Any(
                blackListed => directoryInfo.Attributes.HasFlag(blackListed));

            return isBlacklistedByFullName || blackListedByStartName || isBlackListedByAttributes;
        }
    }
}