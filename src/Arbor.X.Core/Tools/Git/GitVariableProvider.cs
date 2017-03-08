using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

using JetBrains.Annotations;
using NuGet.Versioning;


namespace Arbor.X.Core.Tools.Git
{
    [UsedImplicitly]
    public class GitVariableProvider :IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {

            var variables = new List<IVariable>();

            string branchName = buildVariables.Require(WellKnownVariables.BranchName).ThrowIfEmptyValue().Value;

            if (branchName.StartsWith("refs/heads/"))
            {
                variables.Add(new EnvironmentVariable(WellKnownVariables.BranchFullName, branchName));
            }

            string logicalName = BranchHelper.GetLogicalName(branchName).Name;

            variables.Add(new EnvironmentVariable(WellKnownVariables.BranchLogicalName, logicalName));

            if (BranchHelper.BranchNameHasVersion(branchName))
            {
                string version = BranchHelper.BranchSemVerMajorMinorPatch(branchName).ToString();

                logger.WriteDebug($"Branch has version {version}");

                variables.Add(new EnvironmentVariable(WellKnownVariables.BranchNameVersion, version));

                if (buildVariables.GetBooleanByKey(WellKnownVariables.BranchNameVersionOverrideEnabled, false))
                {
                    logger.WriteVerbose(
                        $"Variable '{WellKnownVariables.BranchNameVersionOverrideEnabled}' is set to true, using version number '{version}' from branch");

                    var semVer = SemanticVersion.Parse(version);

                    var major = semVer.Major.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(
                        $"Overriding {WellKnownVariables.VersionMajor} from '{Environment.GetEnvironmentVariable(WellKnownVariables.VersionMajor)}' to '{major}'");
                    Environment.SetEnvironmentVariable(WellKnownVariables.VersionMajor,
                        major);
                    variables.Add(new EnvironmentVariable(WellKnownVariables.VersionMajor, major));

                    var minor = semVer.Minor.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(
                        $"Overriding {WellKnownVariables.VersionMinor} from '{Environment.GetEnvironmentVariable(WellKnownVariables.VersionMinor)}' to '{minor}'");
                    Environment.SetEnvironmentVariable(WellKnownVariables.VersionMinor,
                        minor);
                    variables.Add(new EnvironmentVariable(WellKnownVariables.VersionMinor, minor));

                    var patch = semVer.Patch.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(
                        $"Overriding {WellKnownVariables.VersionPatch} from '{Environment.GetEnvironmentVariable(WellKnownVariables.VersionPatch)}' to '{patch}'");
                    Environment.SetEnvironmentVariable(WellKnownVariables.VersionPatch,
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

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }

        public int Order => -1;
    }

}
