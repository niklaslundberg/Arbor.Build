using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using JetBrains.Annotations;
using NuGet.Versioning;

namespace Arbor.X.Core.Tools.Git
{
    [UsedImplicitly]
    public class GitVariableProvider : IVariableProvider
    {
        public int Order { get; } = -1;

        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(
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
                logger.WriteDebug("Branch is develop branch, checking if release build is explicitely set");
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

                    var environmentVariable = new EnvironmentVariable(
                        WellKnownVariables.GitHash,
                        gitCommitHash);

                    logger.WriteDebug(
                        $"Setting commit hash variable '{WellKnownVariables.GitHash}' from TeamCity variable '{WellKnownVariables.TeamCity.TeamCityVcsNumber}', value '{gitCommitHash}'");

                    variables.Add(environmentVariable);
                }
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }
}
