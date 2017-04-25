using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Arbor.X.Core.Tools.EnvironmentVariables;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(52)]
    [UsedImplicitly]
    public class NuGetEnvironmentVerification : EnvironmentVerification
    {
        public NuGetEnvironmentVerification()
        {
            RequiredValues.Add(WellKnownVariables.ExternalTools_NuGet_ExePath);
        }

        private async Task EnsureMinNuGetVersionAsync(string nuGetExePath, ILogger logger)
        {
            Action<string, string> nullLogger = (s, s1) => { };
            var standardOut = new List<string>();
            ILogger versionLogger = new DelegateLogger(
                (message, category) => standardOut.Add(message),
                nullLogger,
                nullLogger);

            IEnumerable<string> args = new List<string>();
            ExitCode versionExitCode = await ProcessHelper.ExecuteAsync(nuGetExePath, args, versionLogger);

            if (!versionExitCode.IsSuccess)
            {
                logger.WriteWarning($"NuGet version exit code was {versionExitCode}");
                return;
            }

            string nugetVersion = "NuGet Version: ";
            string versionLine =
                standardOut.FirstOrDefault(
                    line => line.StartsWith(nugetVersion, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrWhiteSpace(versionLine))
            {
                logger.WriteWarning("Could not ensure NuGet version, no version line in NuGet output");
                return;
            }

            char majorNuGetVersion = versionLine.Substring(nugetVersion.Length).FirstOrDefault();

            if (majorNuGetVersion == '2')
            {
                IEnumerable<string> updateSelfArgs = new List<string> { "update", "-self" };
                ExitCode exitCode = await ProcessHelper.ExecuteAsync(nuGetExePath, updateSelfArgs, logger);

                if (!exitCode.IsSuccess)
                {
                    logger.WriteWarning($"The NuGet version could not be determined, exit code {exitCode}");
                }

                return;
            }

            if (majorNuGetVersion != '3')
            {
                logger.WriteWarning(
                    $"NuGet version could not be determined, major version starts with character {majorNuGetVersion}");
                return;
            }

            logger.WriteVerbose($"NuGet major version is {majorNuGetVersion}");
        }

        protected override async Task<bool> PostVariableVerificationAsync(
            StringBuilder variableBuilder,
            IReadOnlyCollection<IVariable> buildVariables,
            ILogger logger)
        {
            IVariable variable =
                buildVariables.SingleOrDefault(item => item.Key == WellKnownVariables.ExternalTools_NuGet_ExePath);

            if (variable == null)
            {
                return false;
            }

            string nuGetExePath = variable.Value;

            bool fileExists = File.Exists(nuGetExePath);

            if (!fileExists)
            {
                variableBuilder.AppendLine($"NuGet.exe path '{nuGetExePath}' does not exist");
            }
            else
            {
                bool nuGetUpdateEnabled =
                    buildVariables.GetBooleanByKey(WellKnownVariables.NuGetSelfUpdateEnabled, true);

                if (nuGetUpdateEnabled)
                {
                    logger.WriteVerbose(
                        $"NuGet self update is enabled by variable '{WellKnownVariables.NuGetSelfUpdateEnabled}'");

                    await EnsureMinNuGetVersionAsync(nuGetExePath, logger);
                }
                else
                {
                    logger.WriteVerbose(
                        $"NuGet self update is disabled by variable '{WellKnownVariables.NuGetSelfUpdateEnabled}'");
                }
            }

            return fileExists;
        }
    }
}
