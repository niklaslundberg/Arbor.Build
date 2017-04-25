using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;

using JetBrains.Annotations;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(450)]
    [UsedImplicitly]
    public class VsTestRunner : ITool
    {
        private string _sourceRoot;

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var enabled = buildVariables.GetBooleanByKey(WellKnownVariables.VSTestEnabled, defaultValue: true);

            if (!enabled)
            {
                logger.WriteWarning("VSTest not enabled");
                return ExitCode.Success;
            }

            var reportPath =
                buildVariables.Require(WellKnownVariables.ExternalTools_VSTest_ReportPath).ThrowIfEmptyValue().Value;
            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var vsTestExePath = buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_VSTest_ExePath,
                defaultValue: string.Empty);

            if (string.IsNullOrWhiteSpace(vsTestExePath))
            {
                logger.WriteWarning(
                    $"{WellKnownVariables.ExternalTools_VSTest_ExePath} is not defined, cannot run any VSTests");
                return ExitCode.Success;
            }

            var ignoreTestFailures = buildVariables.GetBooleanByKey(
                WellKnownVariables.IgnoreTestFailures,
                defaultValue: false);

            bool runTestsInReleaseConfiguration =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled,
                    defaultValue: true);

            if (ignoreTestFailures)
            {
                try
                {
                    return await RunVsTestAsync(logger, reportPath, vsTestExePath, runTestsInReleaseConfiguration);
                }
                catch (Exception ex)
                {
                    logger.WriteWarning($"Ignoring test exception: {ex}");
                }
                return ExitCode.Success;
            }

            return await RunVsTestAsync(logger, reportPath, vsTestExePath, runTestsInReleaseConfiguration);
        }

        private async Task<ExitCode> RunVsTestAsync(ILogger logger, string vsTestReportDirectoryPath, string vsTestExePath, bool runTestsInReleaseConfiguration)
        {
            Type testClassAttribute = typeof(TestClassAttribute);
            Type testMethodAttribute = typeof(TestMethodAttribute);

            var directory = new DirectoryInfo(_sourceRoot);

            var typesToFind = new List<Type> { testClassAttribute, testMethodAttribute };

            List<string> vsTestConsoleArguments =
                new UnitTestFinder(typesToFind).GetUnitTestFixtureDlls(directory, runTestsInReleaseConfiguration).ToList();

            if (!vsTestConsoleArguments.Any())
            {
                logger.WriteWarning(
                    $"Could not find any VSTest tests in directory '{directory.FullName}' or any sub-directory");
                return ExitCode.Success;
            }

            IEnumerable<string> options = GetVsTestConsoleOptions();

            vsTestConsoleArguments.AddRange(options);

            EnsureTestReportDirectoryExists(vsTestReportDirectoryPath);

            var oldCurrentDirectory = SaveCurrentDirectory();

            SetCurrentDirectory(vsTestReportDirectoryPath);

            LogExecution(logger, vsTestConsoleArguments, vsTestExePath);

            try
            {
                Task<ExitCode> execute = ProcessRunner.ExecuteAsync(
                    vsTestExePath,
                    arguments: vsTestConsoleArguments,
                    standardOutLog: logger.Write,
                    standardErrorAction: logger.WriteError,
                    toolAction: logger.Write);

                ExitCode result = await execute;

                return result;
            }
            finally
            {
                RestoreCurrentDirectory(oldCurrentDirectory);
            }
        }

        private void RestoreCurrentDirectory(string currentDirectory)
        {
            Directory.SetCurrentDirectory(currentDirectory);
        }

        private string SaveCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        private void SetCurrentDirectory(string vsTestReportDirectoryPath)
        {
            Directory.SetCurrentDirectory(vsTestReportDirectoryPath);
        }

        private void EnsureTestReportDirectoryExists(string vsTestReportDirectoryPath)
        {
            if (!Directory.Exists(vsTestReportDirectoryPath))
            {
                Directory.CreateDirectory(vsTestReportDirectoryPath);
            }
        }

        private static void LogExecution(ILogger logger, IEnumerable<string> arguments, string exePath)
        {
            var args = string.Join(" ", arguments.Select(item => $"\"{item}\""));
            logger.Write($"Running VSTest {exePath} {args}");
        }

        private static IEnumerable<string> GetVsTestConsoleOptions()
        {
            var options = new List<string> { "/Logger:trx" };
            return options;
        }
    }
}
