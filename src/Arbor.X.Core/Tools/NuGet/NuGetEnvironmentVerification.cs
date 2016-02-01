using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
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

        protected override async Task<bool> PostVariableVerificationAsync(
            StringBuilder stringBuilder,
            IReadOnlyCollection<IVariable> buildVariables,
            ILogger logger)
        {
            var variable =
                buildVariables.SingleOrDefault(item => item.Key == WellKnownVariables.ExternalTools_NuGet_ExePath);

            if (variable == null)
            {
                return false;
            }

            var nuGetExePath = variable.Value;

            var fileExists = File.Exists(nuGetExePath);

            if (!fileExists)
            {
                stringBuilder.AppendLine($"NuGet.exe path '{nuGetExePath}' does not exist");
            }
            else
            {
                var nuGetUpdateEnabled =
                    buildVariables.GetBooleanByKey(WellKnownVariables.NuGetSelfUpdateEnabled, defaultValue: true);

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

        private async Task EnsureMinNuGetVersionAsync(string nuGetExePath, ILogger logger)
        {
            Action<string, string> nullLogger = (s, s1) => { };
            var standardOut = new List<string>();
            ILogger versionLogger = new DelegateLogger(
                (message, category) => standardOut.Add(message),
                warning: nullLogger,
                error: nullLogger);

            IEnumerable<string> args = new List<string>();
            ExitCode versionExitCode = await ProcessRunner.ExecuteAsync(nuGetExePath, arguments: args, logger: versionLogger);

            if (!versionExitCode.IsSuccess)
            {
                logger.WriteWarning($"NuGet version exit code was {versionExitCode}");
                return;
            }

            var nugetVersion = "NuGet Version: ";
            var versionLine =
                standardOut.FirstOrDefault(
                    line => line.StartsWith(nugetVersion, StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrWhiteSpace(versionLine))
            {
                logger.WriteWarning("Could not ensure NuGet version, no version line in NuGet output");
                return;
            }

            var majorNuGetVersion = versionLine.Substring(nugetVersion.Length).FirstOrDefault();

            if (majorNuGetVersion == '2')
            {
                IEnumerable<string> updateSelfArgs = new List<string> { "update", "-self" };
                ExitCode exitCode = await ProcessRunner.ExecuteAsync(nuGetExePath, updateSelfArgs, logger);

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
    }
}
