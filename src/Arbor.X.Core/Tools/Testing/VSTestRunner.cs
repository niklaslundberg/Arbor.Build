using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Properties;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;

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
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.VSTestEnabled, true);

            if (!enabled)
            {
                logger.Warning("VSTest not enabled");
                return ExitCode.Success;
            }

            string reportPath =
                buildVariables.Require(WellKnownVariables.ExternalTools_VSTest_ReportPath).ThrowIfEmptyValue().Value;
            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string vsTestExePath = buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_VSTest_ExePath,
                string.Empty);

            if (string.IsNullOrWhiteSpace(vsTestExePath))
            {
                logger.Warning("{ExternalTools_VSTest_ExePath} is not defined, cannot run any VSTests",
                    WellKnownVariables.ExternalTools_VSTest_ExePath);
                return ExitCode.Success;
            }

            bool ignoreTestFailures = buildVariables.GetBooleanByKey(
                WellKnownVariables.IgnoreTestFailures,
                false);

            bool? runTestsInReleaseConfiguration =
                buildVariables.GetOptionalBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled);

            ImmutableArray<string> assemblyFilePrefix = buildVariables.AssemblyFilePrefixes();

            if (ignoreTestFailures)
            {
                try
                {
                    return await RunVsTestAsync(logger,
                        reportPath,
                        vsTestExePath,
                        runTestsInReleaseConfiguration,
                        assemblyFilePrefix).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Ignoring test exception: {Ex}");
                }

                return ExitCode.Success;
            }

            return await RunVsTestAsync(logger,
                reportPath,
                vsTestExePath,
                runTestsInReleaseConfiguration,
                assemblyFilePrefix).ConfigureAwait(false);
        }

        private static void LogExecution(ILogger logger, IEnumerable<string> arguments, string exePath)
        {
            string args = string.Join(" ", arguments.Select(item => $"\"{item}\""));
            logger.Information("Running VSTest {ExePath} {Args}", exePath, args);
        }

        private static IEnumerable<string> GetVsTestConsoleOptions()
        {
            var options = new List<string> { "/Logger:trx" };
            return options;
        }

        private async Task<ExitCode> RunVsTestAsync(
            ILogger logger,
            string vsTestReportDirectoryPath,
            string vsTestExePath,
            bool? runTestsInReleaseConfiguration,
            ImmutableArray<string> assemblyFilePrefix)
        {
            Type testClassAttribute = typeof(TestClassAttribute);
            Type testMethodAttribute = typeof(TestMethodAttribute);

            var directory = new DirectoryInfo(_sourceRoot);

            var typesToFind = new List<Type> { testClassAttribute, testMethodAttribute };

            List<string> vsTestConsoleArguments =
                new UnitTestFinder(typesToFind).GetUnitTestFixtureDlls(directory,
                        runTestsInReleaseConfiguration,
                        assemblyFilePrefix,
                        FrameworkConstants.NetFramework)
                    .ToList();

            if (vsTestConsoleArguments.Count == 0)
            {
                logger.Warning("Could not find any VSTest tests in directory '{FullName}' or any sub-directory",
                    directory.FullName);
                return ExitCode.Success;
            }

            logger.Debug("Found [{VsTestConsoleArguments}] potential Assembly dll files with tests: {NewLine}: {V}",
                vsTestConsoleArguments,
                Environment.NewLine,
                string.Join(Environment.NewLine, vsTestConsoleArguments.Select(dll => $" * '{dll}'")));

            IEnumerable<string> options = GetVsTestConsoleOptions();

            vsTestConsoleArguments.AddRange(options);

            EnsureTestReportDirectoryExists(vsTestReportDirectoryPath);

            string oldCurrentDirectory = SaveCurrentDirectory();

            SetCurrentDirectory(vsTestReportDirectoryPath);

            LogExecution(logger, vsTestConsoleArguments, vsTestExePath);

            try
            {
                Task<ExitCode> execute = ProcessRunner.ExecuteAsync(
                    vsTestExePath,
                    arguments: vsTestConsoleArguments,
                    standardOutLog: logger.Information,
                    standardErrorAction: logger.Error,
                    toolAction: logger.Information);

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
    }
}
