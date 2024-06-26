﻿using System;
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
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;
using Arbor.FS;

namespace Arbor.Build.Core.Tools.Git;

[UsedImplicitly]
public class BranchNameVariableProvider(
    ILogger classLogger,
    IEnvironmentVariables environmentVariables,
    ISpecialFolders specialFolders,
    IFileSystem fileSystem,
    GitHelper gitHelper)
    : IVariableProvider
{
    public int Order => VariableProviderOrder.Priority - 2;

    public async Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        var possibleVariables = new List<string>
        {
            WellKnownVariables.BranchName, WellKnownVariables.GitHubBranchName
        };

        string? branchName = default;

        foreach (string possibleVariable in possibleVariables)
        {
            branchName = environmentVariables.GetEnvironmentVariable(possibleVariable);

            if (!string.IsNullOrWhiteSpace(branchName))
            {
                logger.Information("Found branch name '{BranchName}' from environment variable '{VariableName}'", branchName, possibleVariable);
                break;
            }

            branchName = buildVariables.GetVariableValueOrDefault(possibleVariable);

            if (!string.IsNullOrWhiteSpace(branchName))
            {
                logger.Information("Found branch name '{BranchName}' from build variable '{VariableName}'", branchName, possibleVariable);
                break;
            }
        }

        var variables = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(branchName))
        {
            logger.Verbose("There is no branch name defined in the environment variables, asking Git");
            Tuple<int, string> branchNameResult = await GetBranchNameByAskingGitExeAsync();

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
            if (logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
            {
                logger.Verbose(
                    "Branch name is defined as '{BranchName}' from environment variable '{EnvironmentVariable}",
                    branchName,
                    WellKnownVariables.BranchName);
            }
        }

        return variables.Select(pair => (IVariable)new BuildVariable(pair.Key, pair.Value)).ToImmutableArray();
    }

    private async Task<Tuple<int, string>> GetBranchNameByAskingGitExeAsync()
    {
        classLogger.Information("Environment variable '{BranchName}' is not defined or has empty value",
            WellKnownVariables.BranchName);

        UPath gitExePath = gitHelper.GetGitExePath(classLogger, specialFolders, environmentVariables);

        if (!fileSystem.FileExists(gitExePath))
        {
            classLogger.Debug("The git path '{GitExePath}' does not exist", gitExePath);

            var githubForWindowsPath =
                UPath.Combine(specialFolders.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ParseAsPath(), "GitHub");

            if (fileSystem.DirectoryExists(githubForWindowsPath))
            {
                var shellFile = UPath.Combine(githubForWindowsPath, "shell.ps1");

                if (fileSystem.FileExists(shellFile))
                {
                    await using var fs = fileSystem.OpenFile(shellFile, FileMode.Open, FileAccess.Read);

                    var lines = await fs.ReadAllLinesAsync();

                    string? pathLine = lines.SingleOrDefault(
                        line => line.Contains("$env:github_git = ", StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(pathLine))
                    {
                        var directory = pathLine.Split('=').Last().Replace("\"", string.Empty, StringComparison.Ordinal).ParseAsPath();

                        var gitPath = UPath.Combine(directory, "bin", "git.exe");

                        if (fileSystem.FileExists(gitPath))
                        {
                            gitExePath = gitPath;
                        }
                    }
                }
            }

            if (!fileSystem.FileExists(gitExePath))
            {
                classLogger.Error("Could not find Git. '{GitExePath}' does not exist", gitExePath);
                return Tuple.Create(-1, string.Empty);
            }
        }

        string? currentDirectory = VcsPathHelper.FindVcsRootPath(Directory.GetCurrentDirectory());

        if (currentDirectory is null)
        {
            classLogger.Error("Could not find source root");
            return Tuple.Create(-1, string.Empty);
        }

        string branchName = await GetGitBranchNameAsync(currentDirectory, gitExePath);

        if (string.IsNullOrWhiteSpace(branchName))
        {
            classLogger.Error("Git branch name was null or empty");
            return Tuple.Create(-1, string.Empty);
        }

        return Tuple.Create(0, branchName);
    }

    private async Task<string> GetGitBranchNameAsync(
        string currentDirectory,
        UPath gitExePath)
    {
        var argumentsLists = new List<List<string>>
        {
            new()
            {
                "rev-parse",
                "--abbrev-ref",
                "HEAD"
            },
            new() { "status --porcelain --branch" }
        };

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
                                fileSystem.ConvertPathToInternal(gitExePath),
                                arguments: argumentsList,
                                standardOutLog: (message, _) =>
                                {
                                    classLogger.Debug("{Message}", message);
                                    gitBranchBuilder.AppendLine(message);
                                },
                                standardErrorAction: classLogger.Error,
                                toolAction: classLogger.Information,
                                cancellationToken: cancellationTokenSource.Token);
                }

                if (!exitCode.IsSuccess)
                {
                    classLogger.Warning("Could not get Git branch name. Git process exit code: {Result}", exitCode);
                }
                else
                {
                    string firstLine = gitBranchBuilder.ToString()
                        .Trim()
                        .Split(
                            [Environment.NewLine],
                            StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault() ?? string.Empty;

                    string? mayBeBranchName = firstLine.GetBranchName();

                    if (!string.IsNullOrWhiteSpace(mayBeBranchName))
                    {
                        return mayBeBranchName;
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
            classLogger.Error("Could not get Git branch name.");
        }

        return branchName;
    }
}