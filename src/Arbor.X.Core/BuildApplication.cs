using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Arbor.X.Core.Tools;
using Arbor.X.Core.Tools.Artifacts;
using Arbor.X.Core.Tools.Environments;
using Arbor.X.Core.Tools.Git;
using Arbor.X.Core.Tools.ILMerge;
using Arbor.X.Core.Tools.Kudu;
using Arbor.X.Core.Tools.MSBuild;
using Arbor.X.Core.Tools.NuGet;
using Arbor.X.Core.Tools.Symbols;
using Arbor.X.Core.Tools.TeamCity;
using Arbor.X.Core.Tools.Testing;
using Arbor.X.Core.Tools.Versioning;
using Arbor.X.Core.Tools.VisualStudio;

namespace Arbor.X.Core
{
    public class BuildApplication
    {
        readonly ILogger _logger;
        CancellationToken _cancellationToken;

        public BuildApplication(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<ExitCode> RunAsync()
        {
            _logger.Write("Using logger '" + _logger.GetType() + "' with log level " + _logger.LogLevel);
            _cancellationToken = CancellationToken.None;
            ExitCode exitCode;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                ExitCode systemToolsResult = await RunSystemToolsAsync();

                if (!systemToolsResult.IsSuccess)
                {
                    const string toolsMessage = "All system tools did not succeed";
                    _logger.WriteError(toolsMessage);

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

            Console.WriteLine("Arbor.X.Build total elapsed time in seconds: {0}",
                stopwatch.Elapsed.TotalSeconds.ToString("F"));

            var exitDelayInMilliseconds =
                Environment.GetEnvironmentVariable(WellKnownVariables.BuildApplicationExitDelayInMilliseconds).TryParseInt32(0);

            if (exitDelayInMilliseconds > 0)
            {
                _logger.Write(string.Format("Delaying build application exit with {0} milliseconds specified in '{1}'", exitDelayInMilliseconds, WellKnownVariables.BuildApplicationExitDelayInMilliseconds));
                await Task.Delay(TimeSpan.FromMilliseconds(exitDelayInMilliseconds), _cancellationToken);
            }

            return exitCode;
        }

        async Task<ExitCode> RunSystemToolsAsync()
        {
            List<IVariable> buildVariables = (await GetBuildVariablesAsync()).ToList();

            string variableAsTable = WellKnownVariables.AllVariables
                .Select(variable => new Dictionary<string, string>
                                    {
                                        {"Name", variable.InvariantName},
                                        {"Description", variable.Description},
                                        {"Default value", variable.DefaultValue}
                                    })
                .DisplayAsTable();
            var environmentVariables = Environment.GetEnvironmentVariables();

            buildVariables.ForEach(variable =>
            {
                if (!environmentVariables.Contains(variable.Key))
                {
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
                }
            });

            _logger.Write(string.Format("{0}Available wellknown variables: {0}{0}{1}", Environment.NewLine, variableAsTable));

            _logger.Write(string.Format("{1}Defined build variables: [{0}] {1}{1}{2}", buildVariables.Count,
                Environment.NewLine,
                buildVariables.Print()));

            IReadOnlyCollection<ToolWithPriority> toolWithPriorities = ToolFinder.GetTools(_logger);

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

                _logger.Write(Environment.NewLine +
                              string.Format("######## Running tool {0} ########", toolWithPriority));

                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    ExitCode toolResult =
                        await toolWithPriority.Tool.ExecuteAsync(_logger, buildVariables, _cancellationToken);

                    stopwatch.Stop();

                    if (toolResult.IsSuccess)
                    {
                        _logger.Write(string.Format("The tool {0} succeeded with exit code {1}", toolWithPriority,
                            toolResult));

                        toolResults.Add(new ToolResult(toolWithPriority, ToolResultType.Succeeded, executionTime: stopwatch.Elapsed));
                    }
                    else
                    {
                        _logger.WriteError(string.Format("The tool {0} failed with exit code {1}", toolWithPriority,
                            toolResult));
                        result = toolResult.Result;

                        toolResults.Add(new ToolResult(toolWithPriority, ToolResultType.Failed,
                            "failed with exit code " + toolResult, executionTime: stopwatch.Elapsed));
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    toolResults.Add(new ToolResult(toolWithPriority, ToolResultType.Failed,
                        string.Format("threw {0}", ex.GetType().Name), executionTime: stopwatch.Elapsed));
                    _logger.WriteError(string.Format("The tool {0} failed with exception {1}", toolWithPriority, ex));
                    result = 1;
                }
            }

            string resultTable = BuildResults(toolResults);

            _logger.Write(Environment.NewLine + new string('.', 100) + Environment.NewLine + "Tool results:" +
                          Environment.NewLine + resultTable);

            if (result != 0)
            {
                return ExitCode.Failure;
            }

            return ExitCode.Success;
        }

        static string BuildResults(IEnumerable<ToolResult> toolResults)
        {
            const string notRun = "Not run";
            const string succeeded = "Succeeded";
            const string failed = "Failed";

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
                                ? (result.ResultType.IsSuccess ? succeeded : failed)
                                : notRun
                        },
                        {
                            "Execution time",
                            result.ExecutionTime == default(TimeSpan) ? "N/A" : ((int)result.ExecutionTime.TotalMilliseconds).ToString("D") + " ms"
                        },
                        {
                            "Message",
                            result.Message
                        }
                    }).DisplayAsTable();

            return displayTable;
        }

        void LogTools(IReadOnlyCollection<ToolWithPriority> toolWithPriorities)
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine(string.Format("Running tools: [{0}]", toolWithPriorities.Count));
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
                        "Run always", tool.RunAlways ? "Run always" : ""
                    }
                })
                .DisplayAsTable());

            _logger.Write(sb.ToString());
        }

        async Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync()
        {
            var buildVariables = new List<IVariable>();

            IEnumerable<IVariable> result = await RunOnceAsync();

            buildVariables.AddRange(result);

            buildVariables.AddRange(EnvironmentVariableHelper.GetBuildVariablesFromEnvironmentVariables(_logger, buildVariables));

            var providers = new List<IVariableProvider>
                            {
                                new GitVariableProvider(),
                                new TeamCityVariableProvider(),
                                new SourcePathVariableProvider(),
                                new ArtifactsVariableProvider(),
                                new MSBuildVariableProvider(),
                                new NugetVariableProvider(),
                                new VisualStudioVariableProvider(),
                                new VsTestVariableProvider(),
                                new MSpecVariableProvider(),
                                new BuildVersionProvider(),
                                new ILMergeVariableProvider(),
                                new SymbolsVariableProvider(),
                                new BuildAgentVariableProvider(),
                                new KuduEnvironmentVariableProvider()
                            }; //TODO use Autofac

            string displayAsTable =
                providers.Select(item => new Dictionary<string, string> {{"Provider", item.GetType().Name}})
                    .DisplayAsTable();

            _logger.WriteVerbose(string.Format("{1}Available variable providers: [{0}]{1}{1}{2}{1}", providers.Count,
                Environment.NewLine,
                displayAsTable));

            foreach (IVariableProvider provider in providers)
            {
                IEnumerable<IVariable> newVariables =
                    await provider.GetEnvironmentVariablesAsync(_logger, buildVariables, _cancellationToken);

                foreach (IVariable @var in newVariables)
                {
                    if (buildVariables.HasKey(@var.Key))
                    {
                        var existing = buildVariables.Single(bv => bv.Key.Equals(@var.Key));

                        if (string.IsNullOrWhiteSpace(buildVariables.GetVariableValueOrDefault(@var.Key, "")))
                        {
                            if (string.IsNullOrWhiteSpace(@var.Value))
                            {
                                _logger.WriteWarning(string.Format("The build variable {0} already exists with empty value, new value is also empty", @var.Key));
                                continue;
                            }

                            _logger.WriteWarning(string.Format("The build variable {0} already exists with empty value, using new value '{1}'", @var.Key, @var.Value));


                            buildVariables.Remove(existing);
                        }
                        else
                        {
                            if (existing.Value.Equals(@var.Value))
                            {
                                continue;
                            }

                            var variableOverrideEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.VariableOverrideEnabled,
                                defaultValue: false);

                            if (variableOverrideEnabled)
                            {
                                buildVariables.Remove(existing);

                                _logger.Write(string.Format("Flag '{0}' is set to true, existing variable with key '{1}' and value '{2}', replacing the value with '{3}'", WellKnownVariables.VariableOverrideEnabled, existing.Key, @existing.Value, @var.Value));
                            }
                            else
                            {
                                _logger.WriteWarning(string.Format("The build variable '{0}' already exists with value '{1}'. To override variables, set flag '{2}' to true", @var.Key, @var.Value, WellKnownVariables.VariableOverrideEnabled));
                                continue;
                            }
                        }
                    }

                    buildVariables.Add(@var);
                }
            }

            AddCompatibilityVariables(buildVariables);

            var sorted = buildVariables
                .OrderBy(variable => variable.Key)
                .ToList();

            return sorted;
        }

        void AddCompatibilityVariables(List<IVariable> buildVariables)
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
                                           {"Name", buildVariable.Key},
                                           {"Value", buildVariable.Value}
                                       });
                }
                else
                {
                    compatibilities.Add(new Dictionary<string, string>
                                        {
                                            {"Name", buildVariable.Key},
                                            {"Compatibility name", compatibilityName},
                                            {"Value", buildVariable.Value}
                                        });

                    buildVariables.Add(new EnvironmentVariable(compatibilityName, buildVariable.Value));
                }
            }

            if (alreadyDefined.Any())
            {
                _logger.WriteWarning(string.Format("{0}Compatibility build variables alread defined {0}{0}{1}{0}", Environment.NewLine,
                    alreadyDefined.DisplayAsTable()));
            }

            if (compatibilities.Any())
            {
                _logger.WriteVerbose(string.Format("{0}Compatibility build variables added {0}{0}{1}{0}", Environment.NewLine,
                    compatibilities.DisplayAsTable()));
            }

            IVariable arborXBranchName =
                buildVariables.SingleOrDefault(
                    @var => @var.Key.Equals(WellKnownVariables.BranchName, StringComparison.InvariantCultureIgnoreCase));

            if (arborXBranchName != null && !string.IsNullOrWhiteSpace(arborXBranchName.Value))
            {
                const string branchKey = "branch";
                const string branchNameKey = "branchName";

                if (!buildVariables.Any(@var => @var.Key.Equals(branchKey, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.WriteVerbose(
                        string.Format(
                            "Build variable with key '{0}' was not defined, using value from variable key {1} ('{2}')",
                            branchKey, arborXBranchName.Key, arborXBranchName.Value));
                    buildVariables.Add(new EnvironmentVariable(branchKey, arborXBranchName.Value));
                }

                if (
                    !buildVariables.Any(
                        @var => @var.Key.Equals(branchNameKey, StringComparison.InvariantCultureIgnoreCase)))
                {
                    _logger.WriteVerbose(
                        string.Format(
                            "Build variable with key '{0}' was not defined, using value from variable key {1} ('{2}')",
                            branchNameKey, arborXBranchName.Key, arborXBranchName.Value));
                    buildVariables.Add(new EnvironmentVariable(branchNameKey, arborXBranchName.Value));
                }
            }
        }

        async Task<IEnumerable<IVariable>> RunOnceAsync()
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

                variables.Add(WellKnownVariables.BranchName, branchName);
            }

            var configurationFromEnvironment = Environment.GetEnvironmentVariable(WellKnownVariables.Configuration);

            if (string.IsNullOrWhiteSpace(configurationFromEnvironment))
            {
                string configuration = GetConfiguration(branchName);
                
                _logger.WriteVerbose(string.Format("Using configuration '{0}' based on branch name '{1}'", configuration, branchName));

                variables.Add(WellKnownVariables.Configuration, configuration);
            }
            else
            {
                _logger.WriteVerbose(string.Format("Using configuration from environment variable '{0}' with value '{1}'", WellKnownVariables.Configuration, configurationFromEnvironment));
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
                    variablesWithNewLinesBuilder.AppendLine(string.Format("Key {0}: ", keyValuePair.Key));
                    variablesWithNewLinesBuilder.AppendLine(string.Format("'{0}'", keyValuePair.Value));
                }

                _logger.WriteError(variablesWithNewLinesBuilder.ToString());

                throw new InvalidOperationException(variablesWithNewLinesBuilder.ToString());
            }

            return variables.Select(item => new EnvironmentVariable(item.Key, item.Value));
        }

        bool IsReleaseBuild(string branchName)
        {
            if (branchName.StartsWith("release", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        string GetConfiguration(string branchName)
        {
            if (branchName.StartsWith("release", StringComparison.InvariantCultureIgnoreCase))
            {
                return "release";
            }

            return "debug";
        }

        async Task<Tuple<int, string>> GetBranchNameByAskingGitExeAsync()
        {
            _logger.Write(string.Format("Environment variable '{0}' is not defined or has empty value",
                WellKnownVariables.BranchName));

            string gitExePath = GitHelper.GetGitExePath();

            if (!File.Exists(gitExePath))
            {
                string githubForWindowsPath =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitHub");

                if (Directory.Exists(githubForWindowsPath))
                {
                    string shellFile = Path.Combine(githubForWindowsPath, "shell.ps1");

                    if (File.Exists(shellFile))
                    {
                        string[] lines = File.ReadAllLines(shellFile);

                        string pathLine = lines.SingleOrDefault(
                            line => line.IndexOf("$env:github_git = ", StringComparison.InvariantCultureIgnoreCase) >= 0);

                        if (!string.IsNullOrWhiteSpace(pathLine))
                        {
                            string directory = pathLine.Split('=').Last().Replace("\"", "");

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
                    _logger.WriteError(string.Format("Could not find Git. '{0}' does not exist", gitExePath));
                    return Tuple.Create(-1, string.Empty);
                }
            }

            var arguments = new List<string> {"rev-parse", "--abbrev-ref", "HEAD"};


            string currentDirectory = VcsPathHelper.FindVcsRootPath();

            if (currentDirectory == null)
            {
                _logger.WriteError("Could not find source root");
                return Tuple.Create(-1, string.Empty);
            }

            string branchName = await GetGitBranchNameAsync(currentDirectory, gitExePath, arguments);

            if (string.IsNullOrWhiteSpace(branchName))
            {
                _logger.WriteError("Git branch name was null or empty");
                return Tuple.Create(-1, string.Empty);
            }
            return Tuple.Create(0, branchName);
        }

        async Task<string> GetGitBranchNameAsync(string currentDirectory, string gitExePath,
            IEnumerable<string> arguments)
        {
            string branchName;
            var gitBranchBuilder = new StringBuilder();

            string oldCurrentDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(currentDirectory);

                ExitCode result =
                    await
                        ProcessRunner.ExecuteAsync(gitExePath, arguments: arguments,
                            standardErrorAction: _logger.WriteError,
                            standardOutLog: (message, prefix) => gitBranchBuilder.AppendLine(message),
                            cancellationToken: _cancellationToken);

                if (!result.IsSuccess)
                {
                    _logger.WriteError(string.Format("Could not get Git branch name. Git process exit code: {0}", result));
                    return string.Empty;
                }
                else
                {
                    branchName = gitBranchBuilder.ToString().Trim();
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(oldCurrentDirectory);
            }
            return branchName;
        }
    }
}