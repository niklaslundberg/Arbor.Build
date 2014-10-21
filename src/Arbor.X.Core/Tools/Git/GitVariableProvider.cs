using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Semver;

namespace Arbor.X.Core.Tools.Git
{
    public class GitVariableProvider :IVariableProvider
    {
        public Task<IEnumerable<IVariable>> GetEnvironmentVariablesAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {

            var variables = new List<IVariable>();

            var branchName = buildVariables.Require(WellKnownVariables.BranchName).ThrowIfEmptyValue().Value;

            if (branchName.StartsWith("refs/heads/"))
            {
                variables.Add(new EnvironmentVariable(WellKnownVariables.BranchFullName, branchName));
            }

            string logicalName = BranchHelper.GetLogicalName(branchName).Name;

            variables.Add(new EnvironmentVariable(WellKnownVariables.BranchLogicalName, logicalName));

            if (BranchHelper.BranchNameHasVersion(branchName))
            {
                var version = BranchHelper.BranchSemVerMajorMinorPatch(branchName).ToString();

                logger.WriteDebug(string.Format("Branch has version {0}", version));

                variables.Add(new EnvironmentVariable(WellKnownVariables.BranchNameVersion, version));

                if (buildVariables.GetBooleanByKey(WellKnownVariables.BranchNameVersionOverrideEnabled, false))
                {
                    logger.WriteVerbose(
                        string.Format("Variable '{0}' is set to true, using version number '{1}' from branch",
                            WellKnownVariables.BranchNameVersionOverrideEnabled, version));

                    var semVer = SemVersion.Parse(version);

                    var major = semVer.Major.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(string.Format("Overriding {0} from '{1}' to '{2}'",
                        WellKnownVariables.VersionMajor,
                        Environment.GetEnvironmentVariable(WellKnownVariables.VersionMajor),
                        major));
                    Environment.SetEnvironmentVariable(WellKnownVariables.VersionMajor,
                        major);
                    variables.Add(new EnvironmentVariable(WellKnownVariables.VersionMajor, major));

                    var minor = semVer.Minor.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(string.Format("Overriding {0} from '{1}' to '{2}'",
                        WellKnownVariables.VersionMinor,
                        Environment.GetEnvironmentVariable(WellKnownVariables.VersionMinor),
                        minor));
                    Environment.SetEnvironmentVariable(WellKnownVariables.VersionMinor,
                        minor);
                    variables.Add(new EnvironmentVariable(WellKnownVariables.VersionMinor, minor));

                    var patch = semVer.Patch.ToString(CultureInfo.InvariantCulture);
                    logger.WriteVerbose(string.Format("Overriding {0} from '{1}' to '{2}'",
                        WellKnownVariables.VersionPatch,
                        Environment.GetEnvironmentVariable(WellKnownVariables.VersionPatch),
                        patch));
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
                logger.WriteDebug(string.Format("Branch has no version in name"));
            }

            return Task.FromResult<IEnumerable<IVariable>>(variables);
        }
    }

}