using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using NUnit.Framework;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(400)]
    public class NUnitTestRunner : ITool
    {
        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var externalTools = buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue();
            var reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();

            var ignoreTestFailuresVariable =
                buildVariables.SingleOrDefault(key => key.Key == WellKnownVariables.IgnoreTestFailures);

            bool ignoreTestFailures;

            if (ignoreTestFailuresVariable != null &&
                bool.TryParse(ignoreTestFailuresVariable.Value, out ignoreTestFailures))
            {
                var message = string.Format("The exit code from NUnit test was not successful, but the environment variable {0} is set to true, thus returning success", WellKnownVariables.IgnoreTestFailures);
                try
                {
                    var exitCode = await RunNUnitAsync(externalTools, logger, reportPath);

                    if (exitCode.IsSuccess)
                    {
                        return exitCode;
                    }

                    logger.WriteWarning(message);

                    return ExitCode.Success;
                }
                catch (Exception ex)
                {
                    logger.WriteWarning(message + ". " + ex);
                }
                return ExitCode.Success;
            }

            return await RunNUnitAsync(externalTools, logger, reportPath);
        }

        async Task<ExitCode> RunNUnitAsync(IVariable externalTools, ILogger logger, IVariable reportPath)
        {
            Type fixtureAttribute = typeof (TestFixtureAttribute);
            Type testMethodAttribute = typeof (TestAttribute);

            var directory = new DirectoryInfo(VcsPathHelper.FindVcsRootPath());

            var typesToFind = new List<Type> { fixtureAttribute, testMethodAttribute };

            var testDlls = new UnitTestFinder(typesToFind).GetUnitTestFixtureDlls(directory).ToList();

            if (!testDlls.Any())
            {
                logger.WriteWarning(string.Format("Could not find any NUnit tests in directory '{0}' or any sub-directory", directory.FullName));
                return ExitCode.Success;
            }

            string nunitExePath = GetNunitExePath(externalTools);

            var results = new List<Tuple<string, ExitCode>>();

            foreach (var testDll in testDlls)
            {
                var nunitConsoleArguments = new List<string> { testDll };

                string reportFilePath = GetNUnitXmlReportFilePath(reportPath);

                EnsureNUnitReportDirectoryExists(reportFilePath);

                IEnumerable<string> options = GetNUnitConsoleOptions(reportFilePath);

                nunitConsoleArguments.AddRange(options);

                LogExecution(logger, nunitConsoleArguments, nunitExePath);

                var result = await ProcessRunner.ExecuteAsync(nunitExePath, arguments: nunitConsoleArguments, standardOutLog: logger.Write,
                                                               standardErrorAction: logger.WriteError,
                                                               toolAction: logger.Write);

                results.Add(Tuple.Create(testDll, result));
            }

            if (results.All(result => result.Item2.IsSuccess))
            {
                return ExitCode.Success;
            }

            var failedTestsBuilder =new StringBuilder();
            failedTestsBuilder.AppendLine("The following DLL files were not tested successfully:");
            foreach (var result in results.Where(r => !r.Item2.IsSuccess))
            {
                failedTestsBuilder.AppendLine(result.Item1);
            }

            logger.WriteError(failedTestsBuilder.ToString());

            return ExitCode.Failure;
        }

        void EnsureNUnitReportDirectoryExists(string reportFile)
        {
            var fileInfo = new FileInfo(reportFile);

// ReSharper disable AssignNullToNotNullAttribute
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }
// ReSharper restore AssignNullToNotNullAttribute
        }

        static void LogExecution(ILogger logger, IEnumerable<string> nunitArgs, string nunitExe)
        {
            var args = string.Join(" ", nunitArgs.Select(item => string.Format("\"{0}\"", item)));
            logger.Write(string.Format("Running NUnit {0} {1}", nunitExe, args));
        }

        static string GetNunitExePath(IVariable externalTools)
        {
            var nunitExe = Path.Combine(externalTools.Value, "nunit", "nunit-console.exe");
            return nunitExe;
        }

        static IEnumerable<string> GetNUnitConsoleOptions(string reportFile)
        {
            var options = new List<string> {string.Format("/xml:{0}", reportFile), "/framework:net-4.0"};
            return options;
        }

        static string GetNUnitXmlReportFilePath(IVariable reportPath)
        {
            string xmlReportName = string.Format("{0}.xml", Guid.NewGuid());

            var reportFile = Path.Combine(reportPath.Value, "nunit", xmlReportName);

            return reportFile;
        }
    }
}