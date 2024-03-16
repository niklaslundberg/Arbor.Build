using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Bools;
using Arbor.Processing;
using JetBrains.Annotations;
using NuGet.Versioning;
using Serilog;
using Serilog.Core;
using Zio;

namespace Arbor.Build.Core.Tools.Git;

[UsedImplicitly]
public class GitVariableProvider(
    IEnvironmentVariables environmentVariables,
    ISpecialFolders specialFolders,
    IFileSystem fileSystem,
    GitHelper gitHelper)
    : IVariableProvider
{
    public int Order { get; } = -1;

    public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
        ILogger? logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        logger ??= Logger.None;
        var variables = new List<IVariable>();

        string branchName = buildVariables.Require(WellKnownVariables.BranchName).GetValueOrThrow();

        if (branchName.StartsWith("refs/heads/", StringComparison.Ordinal))
        {
            variables.Add(new BuildVariable(WellKnownVariables.BranchFullName, branchName));
        }

        string logicalName = BranchHelper.GetLogicalName(branchName).Name;

        variables.Add(new BuildVariable(WellKnownVariables.BranchLogicalName, logicalName));

        if (BranchHelper.BranchNameHasVersion(branchName, environmentVariables))
        {
            string version = BranchHelper.BranchSemVerMajorMinorPatch(branchName, environmentVariables)!.ToString();

            logger.Debug("Branch has version {Version}", version);

            variables.Add(new BuildVariable(WellKnownVariables.BranchNameVersion, version));

            if (buildVariables.GetBooleanByKey(WellKnownVariables.BranchNameVersionOverrideEnabled))
            {
                logger.Verbose(
                    "Variable '{BranchNameVersionOverrideEnabled}' is set to true, using version number '{Version}' from branch",
                    WellKnownVariables.BranchNameVersionOverrideEnabled,
                    version);

                var semVer = SemanticVersion.Parse(version);

                string major = semVer.Major.ToString(CultureInfo.InvariantCulture);

                logger.Verbose("Overriding {VersionMajor} from '{V}' to '{Major}'",
                    WellKnownVariables.VersionMajor,
                    environmentVariables.GetEnvironmentVariable(WellKnownVariables.VersionMajor),
                    major);

                variables.Add(new BuildVariable(WellKnownVariables.VersionMajor, major));

                string minor = semVer.Minor.ToString(CultureInfo.InvariantCulture);

                logger.Verbose("Overriding {VersionMinor} from '{V}' to '{Minor}'",
                    WellKnownVariables.VersionMinor,
                    environmentVariables.GetEnvironmentVariable(WellKnownVariables.VersionMinor),
                    minor);

                variables.Add(new BuildVariable(WellKnownVariables.VersionMinor, minor));

                string patch = semVer.Patch.ToString(CultureInfo.InvariantCulture);

                logger.Verbose("Overriding {VersionPatch} from '{V}' to '{Patch}'",
                    WellKnownVariables.VersionPatch,
                    environmentVariables.GetEnvironmentVariable(WellKnownVariables.VersionPatch),
                    patch);

                variables.Add(new BuildVariable(WellKnownVariables.VersionPatch, patch));
            }
            else
            {
                logger.Debug("Branch name version override is not enabled");
            }
        }
        else
        {
            logger.Debug("Branch has no version in name");
        }

        if (!buildVariables.HasKey(WellKnownVariables.GitHash))
        {
            if (buildVariables.HasKey(WellKnownVariables.TeamCityVcsNumber))
            {
                string gitCommitHash = buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.TeamCityVcsNumber,
                    string.Empty)!;

                if (!string.IsNullOrWhiteSpace(gitCommitHash))
                {
                    var environmentVariable = new BuildVariable(
                        WellKnownVariables.GitHash,
                        gitCommitHash);

                    logger.Debug(
                        "Setting commit hash variable '{GitHash}' from TeamCity variable '{TeamCityVcsNumber}', value '{GitCommitHash}'",
                        WellKnownVariables.GitHash,
                        WellKnownVariables.TeamCityVcsNumber,
                        gitCommitHash);

                    variables.Add(environmentVariable);
                }
            }

            if (!variables.HasKey(WellKnownVariables.GitHash))
            {
                const string arborBuildGitCommitHashEnabled = "Arbor.Build.GitCommitHashEnabled";

                string? environmentVariable =
                    environmentVariables.GetEnvironmentVariable(arborBuildGitCommitHashEnabled);

                if (!environmentVariable
                        .ParseOrDefault(defaultValue: true))
                {
                    logger.Information(
                        "Git commit hash is disabled by environment variable {ArborXGitcommithashenabled} set to {EnvironmentVariable}",
                        arborBuildGitCommitHashEnabled,
                        environmentVariable);
                }
                else
                {
                    UPath gitExePath = gitHelper.GetGitExePath(logger, specialFolders, environmentVariables);

                    var stringBuilder = new StringBuilder();

                    if (gitExePath != UPath.Empty)
                    {
                        var arguments = new List<string> {"rev-parse", "HEAD"};

                        var exitCode = await ProcessRunner.ExecuteProcessAsync(fileSystem.ConvertPathToInternal(gitExePath),
                            arguments,
                            (message, _) => stringBuilder.Append(message),
                            toolAction: logger.Information,
                            cancellationToken: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

                        if (!exitCode.IsSuccess)
                        {
                            logger.Warning("Could not get Git commit hash");
                        }
                        else
                        {
                            string result = stringBuilder.ToString().Trim();

                            if (!string.IsNullOrWhiteSpace(result))
                            {
                                logger.Information("Found Git commit hash '{Result}' by asking git", result);

                                variables.Add(new BuildVariable(WellKnownVariables.GitHash, result));
                            }
                        }
                    }
                }
            }
        }

        if (!buildVariables.HasKey(WellKnownVariables.GitHash)
            && buildVariables.HasKey(WellKnownVariables.GitHubSha)
            && !variables.HasKey(WellKnownVariables.GitHash)
            && buildVariables.GetVariableValueOrDefault(WellKnownVariables.GitHubSha) is {} hash)
        {
            variables.Add(new BuildVariable(WellKnownVariables.GitHash, hash));
        }

        string? gitHubUrl = buildVariables.GetVariableValueOrDefault("GITHUB_SERVER_URL");
        string? gitHubRepository = buildVariables.GetVariableValueOrDefault("GITHUB_REPOSITORY");

        if (string.IsNullOrWhiteSpace(buildVariables.GetVariableValueOrDefault(WellKnownVariables.RepositoryUrl))
            && !string.IsNullOrWhiteSpace(gitHubUrl)
            && !string.IsNullOrWhiteSpace(gitHubRepository)
           )
        {
            string repositoryUrl = $"{gitHubUrl}/{gitHubRepository}";
            variables.Add(new BuildVariable(WellKnownVariables.RepositoryUrl, repositoryUrl));
        }

        return variables.ToImmutableArray();
    }
}