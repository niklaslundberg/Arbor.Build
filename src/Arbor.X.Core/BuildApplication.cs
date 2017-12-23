using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.Defensive;
using Arbor.Defensive.Collections;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.SystemConfiguration;
using Arbor.KVConfiguration.UserConfiguration;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Parsing;
using Arbor.X.Core.Tools;
using Arbor.X.Core.Tools.Git;
using Autofac;
using JetBrains.Annotations;

namespace Arbor.X.Core
{
    public class BuildApplication
    {
        private CancellationToken _cancellationToken;
        private IContainer _container;
        private ILogger _logger;

        public BuildApplication(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExitCode> RunAsync(string[] args)
        {
            MultiSourceKeyValueConfiguration multiSourceKeyValueConfiguration = KeyValueConfigurationManager.Add(new UserConfiguration())
                .Add(new AppSettingsKeyValueConfiguration())
                .Build();

            StaticKeyValueConfigurationManager.Initialize(multiSourceKeyValueConfiguration);

            bool simulateDebug =
                bool.TryParse(Environment.GetEnvironmentVariable("SimulateDebug"), out bool parsed) && parsed;

            bool debugLoggerEnabled = false;

            if (Debugger.IsAttached || simulateDebug)
            {
                if (simulateDebug)
                {
                    _logger.Write("Simulating debug");
                }

                await StartWithDebuggerAsync(args).ConfigureAwait(false);

                if (debugLoggerEnabled)
                {
                    _logger = new DebugLogger(_logger);
                }
            }

            _container = await BuildBootstrapper.StartAsync();

            _logger.Write($"Using logger '{_logger.GetType()}' with log level {_logger.LogLevel}");
            _cancellationToken = CancellationToken.None;
            ExitCode exitCode;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                ExitCode systemToolsResult = await RunSystemToolsAsync();

                if (!systemToolsResult.IsSuccess)
                {
                    const string ToolsMessage = "All system tools did not succeed";
                    _logger.WriteError(ToolsMessage);

                    exitCode = systemToolsResult;
                }
                else
                {
                    exitCode = ExitCode.Success;
                    _logger.Write("All tools succeeded");
                }
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.ToString());
                exitCode = ExitCode.Failure;
            }

            stopwatch.Stop();

            _logger.Write($"Arbor.X.Build total elapsed time in seconds: {stopwatch.Elapsed.TotalSeconds:F}");

            ParseResult<int> exitDelayInMilliseconds =
                Environment.GetEnvironmentVariable(WellKnownVariables.BuildApplicationExitDelayInMilliseconds)
                    .TryParseInt32(0);

            if (exitDelayInMilliseconds > 0)
            {
                _logger.Write(
                    $"Delaying build application exit with {exitDelayInMilliseconds} milliseconds specified in '{WellKnownVariables.BuildApplicationExitDelayInMilliseconds}'");
                await Task.Delay(TimeSpan.FromMilliseconds(exitDelayInMilliseconds), _cancellationToken);
            }

            if (Debugger.IsAttached)
            {
                WriteDebug($"Exiting build application with exit code {exitCode}");

                if (!debugLoggerEnabled)
                {
                    Debugger.Break();
                }
            }

            return exitCode;
        }

        private static string BuildResults(IEnumerable<ToolResult> toolResults)
        {
            const string NotRun = "Not run";
            const string Succeeded = "Succeeded";
            const string Failed = "Failed";

            string displayTable = toolResults.Select(
                    result =>
                        new Dictionary<string, string>
                        {
                            {
                                "Tool",
                                result.ToolWithPriority.Tool.Name()
                            },
                            {
                                "Result",
                                result.ResultType.WasRun
                                    ? (result.ResultType.IsSuccess ? Succeeded : Failed)
                                    : NotRun
                            },
                            {
                                "Execution time",
                                result.ExecutionTime == default
                                    ? "N/A"
                                    : ((int)result.ExecutionTime.TotalMilliseconds).ToString("D") + " ms"
                            },
                            {
                                "Message",
                                result.Message
                            }
                        })
                .DisplayAsTable();

            return displayTable;
        }

        private async Task StartWithDebuggerAsync([NotNull] string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            string baseDir = VcsPathHelper.FindVcsRootPath(AppDomain.CurrentDomain.BaseDirectory);

            string tempPath = @"C:\Temp\arbor.x";

            var tempDirectory = new DirectoryInfo(Path.Combine(
                tempPath,
                "D",
                DateTime.UtcNow.ToFileTimeUtc().ToString()));

            tempDirectory.EnsureExists();

            WriteDebug($"Using temp directory '{tempDirectory}'");

            await DirectoryCopy.CopyAsync(
                baseDir,
                tempDirectory.FullName,
                pathLookupSpecificationOption: DefaultPaths.DefaultPathLookupSpecification.AddExcludedDirectorySegments(new[] { "paket-files" }),
                rootDir: baseDir);

            var environmentVariables = new Dictionary<string, string>
            {
                [WellKnownVariables.BranchNameVersionOverrideEnabled] = "false",
                [WellKnownVariables.VariableOverrideEnabled] = "true",
                [WellKnownVariables.SourceRoot] = tempDirectory.FullName,
                [WellKnownVariables.ExternalTools] = new DirectoryInfo(Path.Combine(tempDirectory.FullName, "tools", "external")).EnsureExists().FullName,
                [WellKnownVariables.BranchName] = "develop",
                [WellKnownVariables.VersionMajor] = "1",
                [WellKnownVariables.VersionMinor] = "0",
                [WellKnownVariables.VersionPatch] = "51",
                [WellKnownVariables.VersionBuild] = "124",
                [WellKnownVariables.GenericXmlTransformsEnabled] = "true",
                [WellKnownVariables.NuGetPackageExcludesCommaSeparated] = "Arbor.X.Bootstrapper.nuspec",
                [WellKnownVariables.NuGetAllowManifestReWrite] = "false",
                [WellKnownVariables.NuGetSymbolPackagesEnabled] = "false",
                [WellKnownVariables.NugetCreateNuGetWebPackagesEnabled] = "false",
                [WellKnownVariables.RunTestsInReleaseConfigurationEnabled] = "false",
                ["Arbor_X_Tests_DummyWebApplication_Arbor_X_NuGet_Package_CreateNuGetWebPackageForProject_Enabled"] =
                "true",
                [WellKnownVariables.ExternalTools_ILRepack_Custom_ExePath] = @"C:\Tools\ILRepack\ILRepack.exe",
                [WellKnownVariables.NuGetVersionUpdatedEnabled] = @"false",
                [WellKnownVariables.ApplicationMetadataEnabled] = @"true",
                [WellKnownVariables.LogLevel] = "information",
                [WellKnownVariables.NugetCreateNuGetWebPackageFilter] = "Arbor.X.Tests.DummyWebApplication,ABC,",
                [WellKnownVariables.WebJobsExcludedFileNameParts] =
                "Microsoft.Build,Microsoft.CodeAnalysis,Microsoft.CodeDom",
                [WellKnownVariables.WebJobsExcludedDirectorySegments] = "roslyn",
                [WellKnownVariables.AppDataJobsEnabled] = "false",
                [WellKnownVariables.ExternalTools_LibZ_ExePath] = @"C:\Tools\Libz\libz.exe",
                [WellKnownVariables.ExternalTools_LibZ_Enabled] = @"false",
                [WellKnownVariables.WebDeployPreCompilationEnabled] = @"false",
                [WellKnownVariables.ExcludedNuGetWebPackageFiles] = @"bin\roslyn\*.*,bin\Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll",
                [WellKnownVariables.NUnitExePathOverride] = @"C:\Tools\NUnit\nunit3-console.exe",
                [WellKnownVariables.NUnitTransformToJunitEnabled] = @"true",
                [WellKnownVariables.XUnitNetFrameworkEnabled] = "false",
                [WellKnownVariables.NUnitEnabled] = "true",
                [WellKnownVariables.MSpecEnabled] = "true",
                [WellKnownVariables.TestsAssemblyStartsWith] = "Arbor.X.Tests"
            };

            foreach (KeyValuePair<string, string> environmentVariable in environmentVariables)
            {
                Environment.SetEnvironmentVariable(environmentVariable.Key, environmentVariable.Value);
            }

            _logger.LogLevel = LogLevel.Debug;

            WriteDebug("Starting with debugger attached");
        }

        private void WriteDebug(string message)
        {
            Debug.WriteLine(message);
            _logger.WriteDebug(message);
        }

        private async Task<ExitCode> RunSystemToolsAsync()
        {
            List<IVariable> buildVariables = (await GetBuildVariablesAsync()).ToList();

            string variableAsTable = WellKnownVariables.AllVariables
                .OrderBy(item => item.InvariantName)
                .Select(
                    variable => new Dictionary<string, string>
                    {
                        { "Name", variable.InvariantName },
                        { "Description", variable.Description },
                        { "Default value", variable.DefaultValue }
                    })
                .DisplayAsTable();
            IDictionary environmentVariables = Environment.GetEnvironmentVariables();

            buildVariables.ForEach(variable =>
            {
                if (!environmentVariables.Contains(variable.Key))
                {
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
                }
            });

            if (buildVariables.GetBooleanByKey(WellKnownVariables.ShowAvailableVariablesEnabled, true))
            {
                _logger.Write(string.Format(
                    "{0}Available wellknown variables: {0}{0}{1}",
                    Environment.NewLine,
                    variableAsTable));
            }

            if (buildVariables.GetBooleanByKey(WellKnownVariables.ShowDefinedVariablesEnabled, true))
            {
                _logger.Write(string.Format(
                    "{1}Defined build variables: [{0}] {1}{1}{2}",
                    buildVariables.Count,
                    Environment.NewLine,
                    buildVariables.OrderBy(variable => variable.Key).Print()));
            }

            IReadOnlyCollection<ToolWithPriority> toolWithPriorities = ToolFinder.GetTools(_container, _logger);

            LogTools(toolWithPriorities);

            int result = 0;

            var toolResults = new List<ToolResult>();

            foreach (ToolWithPriority toolWithPriority in toolWithPriorities)
            {
                if (result != 0)
                {
                    if (!toolWithPriority.RunAlways)
                    {
                        toolResults.Add(new ToolResult(toolWithPriority, ToolResultType.NotRun));
                        continue;
                    }
                }

                int boxLength = 50;

                char boxCharacter = '#';
                string boxLine = new string(boxCharacter, boxLength);

                string message = string.Format(
                    "{0}{1}{2}{1}{2} Running tool {3}{1}{2}{1}{0}",
                    boxLine,
                    Environment.NewLine,
                    boxCharacter,
                    toolWithPriority);

                _logger.Write(message);

                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    ExitCode toolResult =
                        await toolWithPriority.Tool.ExecuteAsync(_logger, buildVariables, _cancellationToken);

                    stopwatch.Stop();

                    if (toolResult.IsSuccess)
                    {
                        _logger.Write($"The tool {toolWithPriority} succeeded with exit code {toolResult}");

                        toolResults.Add(new ToolResult(
                            toolWithPriority,
                            ToolResultType.Succeeded,
                            executionTime: stopwatch.Elapsed));
                    }
                    else
                    {
                        _logger.WriteError($"The tool {toolWithPriority} failed with exit code {toolResult}");
                        result = toolResult.Result;

                        toolResults.Add(
                            new ToolResult(
                                toolWithPriority,
                                ToolResultType.Failed,
                                $"failed with exit code {toolResult}",
                                stopwatch.Elapsed));
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    toolResults.Add(new ToolResult(
                        toolWithPriority,
                        ToolResultType.Failed,
                        $"threw {ex.GetType().Name}",
                        stopwatch.Elapsed));
                    _logger.WriteError($"The tool {toolWithPriority} failed with exception {ex}");
                    result = 1;
                }
            }

            string resultTable = BuildResults(toolResults);

            _logger.Write(
                $"{Environment.NewLine}{new string('.', 100)}{Environment.NewLine}Tool results:{Environment.NewLine}{resultTable}");

            if (result != 0)
            {
                return ExitCode.Failure;
            }

            return ExitCode.Success;
        }

        private void LogTools(IReadOnlyCollection<ToolWithPriority> toolWithPriorities)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine($"Running tools: [{toolWithPriorities.Count}]");
            sb.AppendLine();

            sb.AppendLine(toolWithPriorities.Select(tool =>
                    new Dictionary<string, string>
                    {
                        {
                            "Tool", tool.Tool.Name()
                        },
                        {
                            "Priority", tool.Priority.ToString(CultureInfo.InvariantCulture)
                        },
                        {
                            "Run always", tool.RunAlways ? "Run always" : string.Empty
                        }
                    })
                .DisplayAsTable());

            _logger.Write(sb.ToString());
        }

        private async Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync()
        {
            var buildVariables = new List<IVariable>();

            if (
                Environment.GetEnvironmentVariable(WellKnownVariables.VariableFileSourceEnabled)
                    .TryParseBool(false))
            {
                _logger.Write(
                    $"The environment variable {WellKnownVariables.VariableFileSourceEnabled} is set to true, using file source to set environment variables");

                var files = new List<string>
                    { "arborx_environmentvariables.json", "arborx_environmentvariables.json.user" };

                foreach (string configFile in files)
                {
                    ExitCode exitCode = EnvironmentVariableHelper.SetEnvironmentVariablesFromFile(_logger, configFile);

                    if (!exitCode.IsSuccess)
                    {
                        throw new InvalidOperationException(
                            $"Could not set environment variables from file, set variable '{WellKnownVariables.VariableFileSourceEnabled}' to false to disabled");
                    }
                }
            }
            else
            {
                _logger.WriteDebug(
                    $"The environment variable {WellKnownVariables.VariableFileSourceEnabled} is not set or false, skipping file source to set environment variables");
            }

            IEnumerable<IVariable> result = await RunOnceAsync().ConfigureAwait(false);

            buildVariables.AddRange(result);

            buildVariables.AddRange(
                EnvironmentVariableHelper.GetBuildVariablesFromEnvironmentVariables(_logger, buildVariables));

            IReadOnlyCollection<IVariableProvider> providers = _container.Resolve<IEnumerable<IVariableProvider>>()
                .OrderBy(provider => provider.Order)
                .ToReadOnlyCollection();

            string displayAsTable =
                providers.Select(item => new Dictionary<string, string> { { "Provider", item.GetType().Name } })
                    .DisplayAsTable();

            _logger.WriteVerbose(string.Format(
                "{1}Available variable providers: [{0}]{1}{1}{2}{1}",
                providers.Count,
                Environment.NewLine,
                displayAsTable));

            foreach (IVariableProvider provider in providers)
            {
                IEnumerable<IVariable> newVariables =
                    await provider.GetEnvironmentVariablesAsync(_logger, buildVariables, _cancellationToken);

                foreach (IVariable var in newVariables)
                {
                    if (buildVariables.HasKey(var.Key))
                    {
                        IVariable existing = buildVariables.Single(bv => bv.Key.Equals(var.Key));

                        if (string.IsNullOrWhiteSpace(buildVariables.GetVariableValueOrDefault(var.Key, string.Empty)))
                        {
                            if (string.IsNullOrWhiteSpace(var.Value))
                            {
                                _logger.WriteWarning(
                                    $"The build variable {var.Key} already exists with empty value, new value is also empty");
                                continue;
                            }

                            _logger.WriteWarning(
                                $"The build variable {var.Key} already exists with empty value, using new value '{var.Value}'");

                            buildVariables.Remove(existing);
                        }
                        else
                        {
                            if (existing.Value.Equals(var.Value))
                            {
                                continue;
                            }

                            bool variableOverrideEnabled = buildVariables.GetBooleanByKey(
                                WellKnownVariables.VariableOverrideEnabled,
                                false);

                            if (variableOverrideEnabled)
                            {
                                buildVariables.Remove(existing);

                                _logger.Write(
                                    $"Flag '{WellKnownVariables.VariableOverrideEnabled}' is set to true, existing variable with key '{existing.Key}' and value '{existing.Value}', replacing the value with '{var.Value}'");
                            }
                            else
                            {
                                _logger.WriteWarning(
                                    $"The build variable '{var.Key}' already exists with value '{var.Value}'. To override variables, set flag '{WellKnownVariables.VariableOverrideEnabled}' to true");
                                continue;
                            }
                        }
                    }

                    buildVariables.Add(var);
                }
            }

            AddCompatibilityVariables(buildVariables);

            List<IVariable> sorted = buildVariables
                .OrderBy(variable => variable.Key)
                .ToList();

            return sorted;
        }

        private void AddCompatibilityVariables(List<IVariable> buildVariables)
        {
            IVariable[] buildVariableArray = buildVariables.ToArray();

            var alreadyDefined = new List<Dictionary<string, string>>();
            var compatibilities = new List<Dictionary<string, string>>();

            foreach (IVariable buildVariable in buildVariableArray)
            {
                if (!buildVariable.Key.StartsWith("Arbor.X", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                string compatibilityName = buildVariable.Key.Replace(".", "_");

                if (
                    buildVariables.Any(
                        bv => bv.Key.Equals(compatibilityName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    alreadyDefined.Add(new Dictionary<string, string>
                    {
                        { "Name", buildVariable.Key },
                        { "Value", buildVariable.Value }
                    });
                }
                else
                {
                    compatibilities.Add(new Dictionary<string, string>
                    {
                        { "Name", buildVariable.Key },
                        { "Compatibility name", compatibilityName },
                        { "Value", buildVariable.Value }
                    });

                    buildVariables.Add(new EnvironmentVariable(compatibilityName, buildVariable.Value));
                }
            }

            if (alreadyDefined.Any())
            {
                _logger.WriteWarning(string.Format(
                    "{0}Compatibility build variables alread defined {0}{0}{1}{0}",
                    Environment.NewLine,
                    alreadyDefined.DisplayAsTable()));
            }

            if (compatibilities.Any())
            {
                _logger.WriteVerbose(string.Format(
                    "{0}Compatibility build variables added {0}{0}{1}{0}",
                    Environment.NewLine,
                    compatibilities.DisplayAsTable()));
            }

            IVariable arborXBranchName =
                buildVariables.SingleOrDefault(
                    var => var.Key.Equals(WellKnownVariables.BranchName, StringComparison.InvariantCultureIgnoreCase));

            if (arborXBranchName != null && !string.IsNullOrWhiteSpace(arborXBranchName.Value))
            {
                const string BranchKey = "branch";
                const string BranchNameKey = "branchName";

                if (!buildVariables.Any(var => var.Key.Equals(BranchKey, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.WriteVerbose(
                        $"Build variable with key '{BranchKey}' was not defined, using value from variable key {arborXBranchName.Key} ('{arborXBranchName.Value}')");
                    buildVariables.Add(new EnvironmentVariable(BranchKey, arborXBranchName.Value));
                }

                if (
                    !buildVariables.Any(
                        var => var.Key.Equals(BranchNameKey, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.WriteVerbose(
                        $"Build variable with key '{BranchNameKey}' was not defined, using value from variable key {arborXBranchName.Key} ('{arborXBranchName.Value}')");
                    buildVariables.Add(new EnvironmentVariable(BranchNameKey, arborXBranchName.Value));
                }
            }
        }

        private async Task<IEnumerable<IVariable>> RunOnceAsync()
        {
            var variables = new Dictionary<string, string>();

            string branchName = Environment.GetEnvironmentVariable(WellKnownVariables.BranchName);

            if (string.IsNullOrWhiteSpace(branchName))
            {
                _logger.WriteVerbose("There is no branch name defined in the environment variables, asking Git");
                Tuple<int, string> branchNameResult = await GetBranchNameByAskingGitExeAsync();

                if (branchNameResult.Item1 != 0)
                {
                    throw new InvalidOperationException("Could not find the branch name");
                }

                branchName = branchNameResult.Item2;

                if (string.IsNullOrWhiteSpace(branchName))
                {
                    throw new InvalidOperationException("Could not find the branch name after asking Git");
                }

                variables.Add(WellKnownVariables.BranchName, branchName);
            }

            string configurationFromEnvironment = Environment.GetEnvironmentVariable(WellKnownVariables.Configuration);

            if (string.IsNullOrWhiteSpace(configurationFromEnvironment))
            {
                string configuration = GetConfiguration(branchName);

                _logger.WriteVerbose($"Using configuration '{configuration}' based on branch name '{branchName}'");

                variables.Add(WellKnownVariables.Configuration, configuration);
            }
            else
            {
                _logger.WriteVerbose(
                    $"Using configuration from environment variable '{WellKnownVariables.Configuration}' with value '{configurationFromEnvironment}'");
                variables.Add(WellKnownVariables.Configuration, configurationFromEnvironment);
            }

            bool isReleaseBuild = IsReleaseBuild(branchName);
            variables.Add(WellKnownVariables.ReleaseBuild, isReleaseBuild.ToString());

            List<KeyValuePair<string, string>> newLines =
                variables.Where(item => item.Value.Contains(Environment.NewLine)).ToList();

            if (newLines.Any())
            {
                var variablesWithNewLinesBuilder = new StringBuilder();

                variablesWithNewLinesBuilder.AppendLine("Variables containing new lines: ");

                foreach (KeyValuePair<string, string> keyValuePair in newLines)
                {
                    variablesWithNewLinesBuilder.AppendLine($"Key {keyValuePair.Key}: ");
                    variablesWithNewLinesBuilder.AppendLine($"'{keyValuePair.Value}'");
                }

                _logger.WriteError(variablesWithNewLinesBuilder.ToString());

                throw new InvalidOperationException(variablesWithNewLinesBuilder.ToString());
            }

            return variables.Select(item => new EnvironmentVariable(item.Key, item.Value));
        }

        private bool IsReleaseBuild(string branchName)
        {
            bool isProductionBranch = new BranchName(branchName).IsProductionBranch();

            return isProductionBranch;
        }

        private string GetConfiguration([NotNull] string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            bool isReleaseBranch = new BranchName(branchName).IsProductionBranch();

            if (isReleaseBranch)
            {
                return "release";
            }

            return "debug";
        }

        private async Task<Tuple<int, string>> GetBranchNameByAskingGitExeAsync()
        {
            _logger.Write($"Environment variable '{WellKnownVariables.BranchName}' is not defined or has empty value");

            string gitExePath = GitHelper.GetGitExePath(_logger);

            if (!File.Exists(gitExePath))
            {
                _logger.WriteDebug($"The git path '{gitExePath}' does not exist");

                string githubForWindowsPath =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitHub");

                if (Directory.Exists(githubForWindowsPath))
                {
                    string shellFile = Path.Combine(githubForWindowsPath, "shell.ps1");

                    if (File.Exists(shellFile))
                    {
                        string[] lines = File.ReadAllLines(shellFile);

                        string pathLine = lines.SingleOrDefault(
                            line => line.IndexOf(
                                        "$env:github_git = ",
                                        StringComparison.InvariantCultureIgnoreCase) >= 0);

                        if (!string.IsNullOrWhiteSpace(pathLine))
                        {
                            string directory = pathLine.Split('=').Last().Replace("\"", string.Empty);

                            string githPath = Path.Combine(directory, "bin", "git.exe");

                            if (File.Exists(githPath))
                            {
                                gitExePath = githPath;
                            }
                        }
                    }
                }

                if (!File.Exists(gitExePath))
                {
                    _logger.WriteError($"Could not find Git. '{gitExePath}' does not exist");
                    return Tuple.Create(-1, string.Empty);
                }
            }

            string currentDirectory = VcsPathHelper.FindVcsRootPath();

            if (currentDirectory == null)
            {
                _logger.WriteError("Could not find source root");
                return Tuple.Create(-1, string.Empty);
            }

            string branchName = await GetGitBranchNameAsync(currentDirectory, gitExePath);

            if (string.IsNullOrWhiteSpace(branchName))
            {
                _logger.WriteError("Git branch name was null or empty");
                return Tuple.Create(-1, string.Empty);
            }

            return Tuple.Create(0, branchName);
        }

        private async Task<string> GetGitBranchNameAsync(
            string currentDirectory,
            [NotNull] string gitExePath)
        {
            var argumentsLists = new List<List<string>>
            {
                new List<string>
                {
                    "rev-parse",
                    "--abbrev-ref",
                    "HEAD"
                },
                new List<string>
                    { "status --porcelain --branch" }
            };

            if (string.IsNullOrWhiteSpace(gitExePath))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(gitExePath));
            }

            string branchName = string.Empty;
            var gitBranchBuilder = new StringBuilder();

            string oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                foreach (List<string> argumentsList in argumentsLists)
                {
                    Directory.SetCurrentDirectory(currentDirectory);

                    ExitCode result =
                        await
                            ProcessRunner.ExecuteAsync(
                                gitExePath,
                                arguments: argumentsList,
                                standardErrorAction: _logger.WriteError,
                                standardOutLog: (message, prefix) =>
                                {
                                    _logger.WriteDebug(message);
                                    gitBranchBuilder.AppendLine(message);
                                },
                                toolAction: _logger.Write,
                                cancellationToken: _cancellationToken);

                    if (!result.IsSuccess)
                    {
                        _logger.WriteWarning($"Could not get Git branch name. Git process exit code: {result}");
                    }
                    else
                    {
                        string firstLine = gitBranchBuilder.ToString()
                                               .Trim()
                                               .Split(
                                                   new[] { Environment.NewLine },
                                                   StringSplitOptions.RemoveEmptyEntries)
                                               .FirstOrDefault() ?? string.Empty;

                        Maybe<string> mayBeBranchName = firstLine.GetBranchName();

                        if (mayBeBranchName.HasValue)
                        {
                            return mayBeBranchName.Value;
                        }
                    }
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
            }

            if (string.IsNullOrWhiteSpace(branchName))
            {
                _logger.WriteError("Could not get Git branch name.");
            }

            return branchName;
        }
    }
}
