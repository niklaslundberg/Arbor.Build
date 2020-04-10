using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Cleanup;
using Arbor.Defensive;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Git
{
    [UsedImplicitly]
    public class BranchNameVariableProvider : IVariableProvider
    {
        private readonly ILogger _logger;

        public BranchNameVariableProvider(ILogger logger) => _logger = logger;

        public int Order => VariableProviderOrder.Priority - 2;

        public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var possibleVariables = new List<string>
            {
                WellKnownVariables.BranchName, WellKnownVariables.GitHubBranchName
            };

            string branchName = default;

            foreach (string possibleVariable in possibleVariables)
            {
                branchName = Environment.GetEnvironmentVariable(possibleVariable);

                if (!string.IsNullOrWhiteSpace(branchName))
                {
                    logger.Information("Found branch name '{BranchName}' from environment variable '{VariableName}'", branchName, possibleVariable);
                    break;
                }

                branchName = buildVariables.GetVariableValueOrDefault(possibleVariable, null);

                if (!string.IsNullOrWhiteSpace(branchName))
                {
                    logger.Information("Found branch name '{BranchName}' from build variable '{VariableName}'", branchName, possibleVariable);
                    break;
                }
            }

            var variables = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(branchName))
            {
                _logger.Verbose("There is no branch name defined in the environment variables, asking Git");
                Tuple<int, string> branchNameResult = await GetBranchNameByAskingGitExeAsync().ConfigureAwait(false);

                if (branchNameResult.Item1 != 0)
                {
                    throw new InvalidOperationException(Resources.TheBranchNameCouldNotBeFound);
                }

                branchName = branchNameResult.Item2;

                if (string.IsNullOrWhiteSpace(branchName))
                {
                    throw new InvalidOperationException(Resources.TheBranchNameCouldNotBeFoundByAskingGit);
                }

                variables.Add(WellKnownVariables.BranchName, branchName);
            }
            else
            {
                if (_logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
                {
                    _logger.Verbose(
                        "Branch name is defined as '{BranchName}' from environment variable '{EnvironmentVariable}",
                        branchName,
                        WellKnownVariables.BranchName);
                }
            }

            return variables.Select(pair => (IVariable)new BuildVariable(pair.Key, pair.Value)).ToImmutableArray();
        }

        private async Task<Tuple<int, string>> GetBranchNameByAskingGitExeAsync()
        {
            _logger.Information("Environment variable '{BranchName}' is not defined or has empty value",
                WellKnownVariables.BranchName);

            string gitExePath = GitHelper.GetGitExePath(_logger);

            if (!File.Exists(gitExePath))
            {
                _logger.Debug("The git path '{GitExePath}' does not exist", gitExePath);

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
                                        StringComparison.OrdinalIgnoreCase) >= 0);

                        if (!string.IsNullOrWhiteSpace(pathLine))
                        {
                            string directory = pathLine.Split('=').Last().Replace("\"", string.Empty, StringComparison.Ordinal);

                            string gitPath = Path.Combine(directory, "bin", "git.exe");

                            if (File.Exists(gitPath))
                            {
                                gitExePath = gitPath;
                            }
                        }
                    }
                }

                if (!File.Exists(gitExePath))
                {
                    _logger.Error("Could not find Git. '{GitExePath}' does not exist", gitExePath);
                    return Tuple.Create(-1, string.Empty);
                }
            }

            string currentDirectory = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());

            if (currentDirectory == null)
            {
                _logger.Error("Could not find source root");
                return Tuple.Create(-1, string.Empty);
            }

            string branchName = await GetGitBranchNameAsync(currentDirectory, gitExePath).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(branchName))
            {
                _logger.Error("Git branch name was null or empty");
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
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(gitExePath));
            }

            string branchName = string.Empty;
            var gitBranchBuilder = new StringBuilder();

            string oldCurrentDirectory = Directory.GetCurrentDirectory();

            try
            {
                foreach (List<string> argumentsList in argumentsLists)
                {
                    Directory.SetCurrentDirectory(currentDirectory);

                    ExitCode exitCode;

                    using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        exitCode =
                            await
                                ProcessRunner.ExecuteProcessAsync(
                                    gitExePath,
                                    arguments: argumentsList,
                                    standardErrorAction: _logger.Error,
                                    standardOutLog: (message, _) =>
                                    {
                                        _logger.Debug("{Message}", message);
                                        gitBranchBuilder.AppendLine(message);
                                    },
                                    toolAction: _logger.Information,
                                    cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);
                    }

                    if (!exitCode.IsSuccess)
                    {
                        _logger.Warning("Could not get Git branch name. Git process exit code: {Result}", exitCode);
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
                _logger.Error("Could not get Git branch name.");
            }

            return branchName;
        }
    }
}
