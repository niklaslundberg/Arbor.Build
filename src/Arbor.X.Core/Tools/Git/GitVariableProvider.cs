using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Bools;
using Arbor.Defensive;
using Arbor.Processing;
using JetBrains.Annotations;
using NuGet.Versioning;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.Tools.Git
{
    [UsedImplicitly]
    public class GitVariableProvider : IVariableProvider
    {
        public int Order { get; } = -1;

        public async Task<ImmutableArray<IVariable>> GetBuildVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            logger = logger ?? Logger.None;
            var variables = new List<IVariable>();

            string branchName = buildVariables.Require(WellKnownVariables.BranchName).ThrowIfEmptyValue().Value;

            if (branchName.StartsWith("refs/heads/", StringComparison.Ordinal))
            {
                variables.Add(new BuildVariable(WellKnownVariables.BranchFullName, branchName));
            }

            string logicalName = BranchHelper.GetLogicalName(branchName).Name;

            variables.Add(new BuildVariable(WellKnownVariables.BranchLogicalName, logicalName));

            Maybe<BranchName> maybeBranch = BranchName.TryParse(logicalName);

            if (maybeBranch.HasValue && maybeBranch.Value.IsDevelopBranch())
            {
                logger.Debug("Branch '{Name}' is develop branch, checking if release build is explicitly set",
                    maybeBranch.Value.Name);
                Maybe<IVariable> releaseBuildEnabled =
                    buildVariables.GetOptionalVariable(WellKnownVariables.ReleaseBuildEnabled);

                if (!releaseBuildEnabled.HasValue
                    || !bool.TryParse(releaseBuildEnabled.Value.Value, out bool isReleaseBuildEnabled))
                {
                    variables.Add(new BuildVariable(WellKnownVariables.ReleaseBuildEnabled, "false"));
                    logger.Debug(
                        "Release build is not explicitly set when branch is develop branch, skipping release build");
                }
                else
                {
                    logger.Debug(
                        "Release build is explicitly set when branch is develop branch, value {IsReleaseBuildEnabled}",
                        isReleaseBuildEnabled);
                }

                Maybe<IVariable> isDebugBuildEnabled =
                    buildVariables.GetOptionalVariable(WellKnownVariables.DebugBuildEnabled);

                if (!isDebugBuildEnabled.HasValue)
                {
                    variables.Add(new BuildVariable(WellKnownVariables.DebugBuildEnabled, "true"));
                }
            }

            if (maybeBranch.HasValue && maybeBranch.Value.IsProductionBranch())
            {
                logger.Debug("Branch is production branch, checking if release build is explicitly set");
                Maybe<IVariable> debugBuildEnabled =
                    buildVariables.GetOptionalVariable(WellKnownVariables.DebugBuildEnabled);

                if (!debugBuildEnabled.HasValue
                    || !bool.TryParse(debugBuildEnabled.Value.Value, out bool isDebugBuildEnabled))
                {
                    variables.Add(new BuildVariable(WellKnownVariables.DebugBuildEnabled, "false"));
                    logger.Debug(
                        "Debug build is not explicitly set when branch is production branch, skipping debug build");
                }
                else
                {
                    logger.Debug(
                        "Debug build is explicitly set when branch is production branch, value {IsDebugBuildEnabled}",
                        isDebugBuildEnabled);
                }

                Maybe<IVariable> releaseBuildEnabled =
                    buildVariables.GetOptionalVariable(WellKnownVariables.ReleaseBuildEnabled);

                if (!releaseBuildEnabled.HasValue)
                {
                    variables.Add(new BuildVariable(WellKnownVariables.ReleaseBuildEnabled, "true"));
                }
            }

            if (BranchHelper.BranchNameHasVersion(branchName))
            {
                string version = BranchHelper.BranchSemVerMajorMinorPatch(branchName).ToString();

                logger.Debug("Branch has version {Version}", version);

                variables.Add(new BuildVariable(WellKnownVariables.BranchNameVersion, version));

                if (buildVariables.GetBooleanByKey(WellKnownVariables.BranchNameVersionOverrideEnabled))
                {
                    logger.Verbose(
                        "Variable '{BranchNameVersionOverrideEnabled}' is set to true, using version number '{Version}' from branch",
                        WellKnownVariables.BranchNameVersionOverrideEnabled,
                        version);

                    SemanticVersion semVer = SemanticVersion.Parse(version);

                    string major = semVer.Major.ToString(CultureInfo.InvariantCulture);

                    logger.Verbose("Overriding {VersionMajor} from '{V}' to '{Major}'",
                        WellKnownVariables.VersionMajor,
                        Environment.GetEnvironmentVariable(WellKnownVariables.VersionMajor),
                        major);

                    variables.Add(new BuildVariable(WellKnownVariables.VersionMajor, major));

                    string minor = semVer.Minor.ToString(CultureInfo.InvariantCulture);

                    logger.Verbose("Overriding {VersionMinor} from '{V}' to '{Minor}'",
                        WellKnownVariables.VersionMinor,
                        Environment.GetEnvironmentVariable(WellKnownVariables.VersionMinor),
                        minor);

                    variables.Add(new BuildVariable(WellKnownVariables.VersionMinor, minor));

                    string patch = semVer.Patch.ToString(CultureInfo.InvariantCulture);

                    logger.Verbose("Overriding {VersionPatch} from '{V}' to '{Patch}'",
                        WellKnownVariables.VersionPatch,
                        Environment.GetEnvironmentVariable(WellKnownVariables.VersionPatch),
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
                        string.Empty);

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
                    const string arborXGitcommithashenabled = "Arbor.X.GitCommitHashEnabled";

                    string environmentVariable = Environment.GetEnvironmentVariable(arborXGitcommithashenabled);

                    if (!environmentVariable
                        .ParseOrDefault(true))
                    {
                        logger.Information(
                            "Git commit hash is disabled by environment variable {ArborXGitcommithashenabled} set to {EnvironmentVariable}",
                            arborXGitcommithashenabled,
                            environmentVariable);
                    }
                    else
                    {
                        string gitExePath = GitHelper.GetGitExePath(logger);

                        var stringBuilder = new StringBuilder();

                        if (!string.IsNullOrWhiteSpace(gitExePath))
                        {
                            var arguments = new List<string> { "rev-parse", "HEAD" };

                            ExitCode exitCode = await ProcessRunner.ExecuteProcessAsync(gitExePath,
                                arguments: arguments,
                                standardOutLog: (message, category) => stringBuilder.Append(message),
                                toolAction: logger.Information,
                                cancellationToken: cancellationToken).ConfigureAwait(false);

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

            return variables.ToImmutableArray();
        }
    }
}
