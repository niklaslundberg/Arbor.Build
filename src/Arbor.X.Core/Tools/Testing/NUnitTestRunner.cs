using System; using Serilog;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Properties;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(400)]
    [UsedImplicitly]
    public class NUnitTestRunner : ITool
    {
        private string _sourceRoot;
        private string _exePathOverride;
        private bool _transformToJunit;

        private static string GetNUnitXmlReportFilePath(IVariable reportPath, string testDll)
        {
            var testDllFile = new FileInfo(testDll);

            string xmlReportName = $"{testDllFile.Name}.xml";

            string reportFile = Path.Combine(reportPath.Value, "nunit", xmlReportName);

            return reportFile;
        }

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.NUnitEnabled, true);

            if (!enabled)
            {
                logger.Warning("NUnit not enabled");
                return ExitCode.Success;
            }

            IVariable externalTools = buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue();
            IVariable reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();

            _exePathOverride = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NUnitExePathOverride, string.Empty);
            _transformToJunit = buildVariables.GetBooleanByKey(WellKnownVariables.NUnitTransformToJunitEnabled, false);

            IVariable ignoreTestFailuresVariable =
                buildVariables.SingleOrDefault(key => key.Key == WellKnownVariables.IgnoreTestFailures);

            bool testsEnabled = buildVariables.GetBooleanByKey(WellKnownVariables.TestsEnabled, true);

            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            if (!testsEnabled)
            {
                logger.Warning("Tests are disabled (build variable '{TestsEnabled}' is false)", WellKnownVariables.TestsEnabled);
                return ExitCode.Success;
            }

            bool? runTestsInReleaseConfiguration =
                buildVariables.GetOptionalBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled);

            bool ignoreTestFailures = ignoreTestFailuresVariable.GetValueOrDefault(false);

            ImmutableArray<string> assemblyFilePrefix = buildVariables.AssemblyFilePrefixes();

            if (ignoreTestFailures)
            {
                string message =
                    $"The exit code from NUnit test was not successful, but the environment variable {WellKnownVariables.IgnoreTestFailures} is set to true, thus returning success";

                try
                {
                    ExitCode exitCode = await RunNUnitAsync(
                        externalTools,
                        logger,
                        reportPath,
                        runTestsInReleaseConfiguration,
                        assemblyFilePrefix).ConfigureAwait(false);

                    if (exitCode.IsSuccess)
                    {
                        return exitCode;
                    }

                    logger.Warning(message);

                    return ExitCode.Success;
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "{Message}. ", message);
                }

                return ExitCode.Success;
            }

            return await RunNUnitAsync(externalTools, logger, reportPath, runTestsInReleaseConfiguration, assemblyFilePrefix).ConfigureAwait(false);
        }

        private static void LogExecution(ILogger logger, IEnumerable<string> nunitArgs, string nunitExe)
        {
            string args = string.Join(" ", nunitArgs.Select(item => $"\"{item}\""));
            logger.Information("Running NUnit {NunitExe} {Args}", nunitExe, args);
        }

        private string GetNunitExePath(IVariable externalTools)
        {
            if (!string.IsNullOrWhiteSpace(_exePathOverride) && File.Exists(_exePathOverride))
            {
                return _exePathOverride;
            }

            string nunitExe = Path.Combine(externalTools.Value, "nunit", "nunit3-console.exe");
            return nunitExe;
        }

        private IEnumerable<string> GetNUnitConsoleOptions(IVariable externalTools, string reportFile)
        {
            string report;

            if (_transformToJunit)
            {
                string junitTransformFile = Path.Combine(externalTools.Value, "nunit", "nunit3-junit.xslt");

                report = $"--result={reportFile};transform={junitTransformFile}";
            }
            else
            {
                report = $"--result={reportFile}";
            }

            var options = new List<string> { report };

            return options;
        }

        private async Task<ExitCode> RunNUnitAsync(
            IVariable externalTools,
            ILogger logger,
            IVariable reportPath,
            bool? runTestsInReleaseConfiguration,
            ImmutableArray<string> assemblyFilePrefix)
        {
            Type fixtureAttribute = typeof(TestFixtureAttribute);
            Type testMethodAttribute = typeof(TestAttribute);

            var directory = new DirectoryInfo(_sourceRoot);

            var typesToFind = new List<Type> { fixtureAttribute, testMethodAttribute };

            Stopwatch stopwatch = Stopwatch.StartNew();

            List<string> testDlls = new UnitTestFinder(typesToFind)
                .GetUnitTestFixtureDlls(directory, runTestsInReleaseConfiguration, assemblyFilePrefix, FrameworkConstants.NetFramework)
                .ToList();

            stopwatch.Stop();

            logger.Information("NUnit test assembly lookup took {ElapsedMilliseconds:F2} milliseconds", stopwatch.ElapsedMilliseconds);

            if (testDlls.Count == 0)
            {
                logger.Warning("Could not find any NUnit tests in directory '{FullName}' or any sub-directory", directory.FullName);
                return ExitCode.Success;
            }

            logger.Debug("Found [{TestDlls}] potential Assembly dll files with tests: {NewLine}: {V}", testDlls, Environment.NewLine, string.Join(Environment.NewLine, testDlls.Select(dll => $" * '{dll}'")));

            string nunitExePath = GetNunitExePath(externalTools);

            var results = new List<Tuple<string, ExitCode>>();

            foreach (string testDll in testDlls)
            {
                var nunitConsoleArguments = new List<string> { testDll };

                string reportFilePath = GetNUnitXmlReportFilePath(reportPath, testDll);

                EnsureNUnitReportDirectoryExists(reportFilePath);

                IEnumerable<string> options = GetNUnitConsoleOptions(externalTools, reportFilePath);

                nunitConsoleArguments.AddRange(options);

                LogExecution(logger, nunitConsoleArguments, nunitExePath);

                Stopwatch executionStopwatch = Stopwatch.StartNew();

                ExitCode result = await ProcessRunner.ExecuteAsync(
                    nunitExePath,
                    arguments: nunitConsoleArguments,
                    standardOutLog: logger.Information,
                    standardErrorAction: logger.Error,
                    toolAction: logger.Information).ConfigureAwait(false);

                executionStopwatch.Stop();

                logger.Information("NUnit execution took {ElapsedMilliseconds:F2} milliseconds", executionStopwatch.ElapsedMilliseconds);

                results.Add(Tuple.Create(testDll, result));
            }

            if (results.All(result => result.Item2.IsSuccess))
            {
                return ExitCode.Success;
            }

            var failedTestsBuilder = new StringBuilder();
            failedTestsBuilder.AppendLine("The following DLL files were not tested successfully:");
            foreach (Tuple<string, ExitCode> result in results.Where(r => !r.Item2.IsSuccess))
            {
                failedTestsBuilder.AppendLine(result.Item1);
            }

            logger.Error(failedTestsBuilder.ToString());

            return ExitCode.Failure;
        }

        private void EnsureNUnitReportDirectoryExists(string reportFile)
        {
            var fileInfo = new FileInfo(reportFile);

// ReSharper disable AssignNullToNotNullAttribute
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }

// ReSharper restore AssignNullToNotNullAttribute
        }
    }
}
