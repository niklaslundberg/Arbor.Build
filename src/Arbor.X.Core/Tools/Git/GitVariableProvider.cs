using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.Logging;
using FubuCore;
using JetBrains.Annotations;
using NuGet.Versioning;

namespace Arbor.X.Core.Tools.Git
{
    [UsedImplicitly]
    public class GitVariableProvider : IVariableProvider
    {
        public int Order { get; } = -1;

        public async Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var variables = new List<IVariable>();

            string branchName = buildVariables.Require(WellKnownVariables.BranchName).ThrowIfEmptyValue().Value;

            if (branchName.StartsWith("refs/heads/", StringComparison.Ordinal))
            {
                variables.Add(new EnvironmentVariable(WellKnownVariables.BranchFullName, branchName));
            }

            string logicalName = BranchHelper.GetLogicalName(branchName).Name;

            variables.Add(new EnvironmentVariable(WellKnownVariables.BranchLogicalName, logicalName));

            Maybe<BranchName> maybeBranch = BranchName.TryParse(logicalName);

            if (maybeBranch.HasValue && maybeBranch.Value.IsDevelopBranch())
            {
                logger.WriteDebug($"Branch '{maybeBranch.Value.Name}' is develop branch, checking if release build is explicitely set");
                Maybe<IVariable> releaseBuildEnabled =
                    buildVariables.GetOptionalVariable(WellKnownVariables.ReleaseBuildEnabled);

                if (!releaseBuildEnabled.HasValue ||
                    !bool.TryParse(releaseBuildEnabled.Value.Value, out bool isReleaseBuildEnabled))
                {
                    variables.Add(new EnvironmentVariable(WellKnownVariables.ReleaseBuildEnabled, "false"));
                    logger.WriteDebug(
                        "Release build is not explicitely set when branch is develop branch, skipping release build");
                }
                else
                {
                    logger.WriteDebug(
                        $"Release build is explicitely set when branch is develop branch, value {isReleaseBuildEnabled}");
                }

                Maybe<IVariable> isDebugBuildEnabled =
                    buildVariables.GetOptionalVariable(WellKnownVariables.DebugBuildEnabled);

                if (!isDebugBuildEnabled.HasValue)
                {
                    variables.Add(new EnvironmentVariable(WellKnownVariables.DebugBuildEnabled, "true"));
                }
            }

            if (maybeBranch.HasValue && maybeBranch.Value.IsProductionBranch())
            {
                logger.WriteDebug("Branch is production branch, checking if release build is explicitely set");
                Maybe<IVariable> debugBuildEnabled =
                    buildVariables.GetOptionalVariable(WellKnownVariables.DebugBuildEnabled);

                if (!debugBuildEnabled.HasValue ||
                    !bool.TryParse(debugBuildEnabled.Value.Value, out bool isDebugBuildEnabled))
                {
                    variables.Add(new EnvironmentVariable(WellKnownVariables.DebugBuildEnabled, "false"));
                    logger.WriteDebug(
                        "Debug build is not explicitely set when branch is production branch, skipping debug build");
                }
                else
                {
                    logger.WriteDebug(
                        $"Debug build is explicitely set when branch is production branch, value {isDebugBuildEnabled}");
                }

                Maybe<IVariable> releaseBuildEnabled =
                    buildVariables.GetOptionalVariable(WellKnownVariables.ReleaseBuildEnabled);

                if (!releaseBuildEnabled.HasValue)
                {
                    variables.Add(new EnvironmentVariable(WellKnownVariables.ReleaseBuildEnabled, "true"));
                }
            }

            if (BranchHelper.BranchNameHasVersion(branchName))
            {
                string version = BranchHelper.BranchSemVerMajorMinorPatch(branchName).ToString();

                logger.WriteDebug($"Branch has version {version}");

                variables.Add(new EnvironmentVariable(WellKnownVariables.BranchNameVersion, version));

                if (buildVariables.GetBooleanByKey(WellKnownVariables.BranchNameVersionOverrideEnabled, false))
                {
                    logger.WriteVerbose(
                        $"Variable '{WellKnownVariables.BranchNameVersionOverrideEnabled}' is set to true, using version number '{version}' from branch");

                    SemanticVersion semVer = SemanticVersion.Parse(version);

                    string major = semVer.Major.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(
                        $"Overriding {WellKnownVariables.VersionMajor} from '{Environment.GetEnvironmentVariable(WellKnownVariables.VersionMajor)}' to '{major}'");
                    Environment.SetEnvironmentVariable(
                        WellKnownVariables.VersionMajor,
                        major);
                    variables.Add(new EnvironmentVariable(WellKnownVariables.VersionMajor, major));

                    string minor = semVer.Minor.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(
                        $"Overriding {WellKnownVariables.VersionMinor} from '{Environment.GetEnvironmentVariable(WellKnownVariables.VersionMinor)}' to '{minor}'");
                    Environment.SetEnvironmentVariable(
                        WellKnownVariables.VersionMinor,
                        minor);
                    variables.Add(new EnvironmentVariable(WellKnownVariables.VersionMinor, minor));

                    string patch = semVer.Patch.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(
                        $"Overriding {WellKnownVariables.VersionPatch} from '{Environment.GetEnvironmentVariable(WellKnownVariables.VersionPatch)}' to '{patch}'");
                    Environment.SetEnvironmentVariable(
                        WellKnownVariables.VersionPatch,
                        patch);
                    variables.Add(new EnvironmentVariable(WellKnownVariables.VersionPatch, patch));
                }
                else
                {
                    logger.WriteDebug("Branch name version override is not enabled");
                }
            }
            else
            {
                logger.WriteDebug("Branch has no version in name");
            }

            if (!buildVariables.HasKey(WellKnownVariables.GitHash))
            {
                if (buildVariables.HasKey(WellKnownVariables.TeamCity.TeamCityVcsNumber))
                {
                    string gitCommitHash = buildVariables.GetVariableValueOrDefault(
                        WellKnownVariables.TeamCity.TeamCityVcsNumber,
                        string.Empty);

                    if (!string.IsNullOrWhiteSpace(gitCommitHash))
                    {
                        var environmentVariable = new EnvironmentVariable(
                            WellKnownVariables.GitHash,
                            gitCommitHash);

                        logger.WriteDebug(
                            $"Setting commit hash variable '{WellKnownVariables.GitHash}' from TeamCity variable '{WellKnownVariables.TeamCity.TeamCityVcsNumber}', value '{gitCommitHash}'");

                        variables.Add(environmentVariable);
                    }
                }

                if (!variables.HasKey(WellKnownVariables.GitHash))
                {
                    string arborXGitcommithashenabled = "Arbor.X.GitCommitHashEnabled";

                    string environmentVariable = Environment.GetEnvironmentVariable(arborXGitcommithashenabled);

                    if (!environmentVariable
                        .TryParseBool(defaultValue: true).Value)
                    {
                        logger.Write(
                            $"Git commit hash is disabled by environment variable {arborXGitcommithashenabled} set to {environmentVariable}");
                    }
                    else
                    {
                        string gitExePath = GitHelper.GetGitExePath(logger);

                        var stringBuilder = new StringBuilder();

                        if (!string.IsNullOrWhiteSpace(gitExePath))
                        {
                            var arguments = new List<string> { "rev-parse", "HEAD" };

                            ExitCode exitCode = await ProcessRunner.ExecuteAsync(gitExePath,
                                arguments: arguments,
                                standardOutLog: (message, category) => stringBuilder.Append(message),
                                toolAction: logger.Write,
                                cancellationToken: cancellationToken);

                            if (!exitCode.IsSuccess)
                            {
                                logger.WriteWarning("Could not get Git commit hash");
                            }
                            else
                            {
                                string result = stringBuilder.ToString().Trim();

                                if (!string.IsNullOrWhiteSpace(result))
                                {
                                    logger.Write($"Found Git commit hash '{result}' by asking git");

                                    variables.Add(new EnvironmentVariable(WellKnownVariables.GitHash, result));
                                }
                            }
                        }
                    }
                }
            }

            return variables;
        }
    }
}
