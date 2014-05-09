using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core.Annotations;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(450), UsedImplicitly]
    public class VsTestRunner : ITool
    {
        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            var reportPath =
                buildVariables.Require(WellKnownVariables.ExternalTools_VSTest_ReportPath).ThrowIfEmptyValue().Value;

            var vsTestExePath = buildVariables.GetVariableValueOrDefault(WellKnownVariables.ExternalTools_VSTest_ExePath, defaultValue: "");

            if (string.IsNullOrWhiteSpace(vsTestExePath))
            {
                logger.WriteWarning(WellKnownVariables.ExternalTools_VSTest_ExePath + " is not defined, cannot run any VSTests");
                return ExitCode.Success;
            }

            var ignoreTestFailures =
                buildVariables.GetBooleanByKey(WellKnownVariables.IgnoreTestFailures, defaultValue: false);

            if (ignoreTestFailures)
            {
                try
                {
                    return await RunVsTestAsync(logger, reportPath, vsTestExePath);
                }
                catch (Exception ex)
                {
                    logger.WriteWarning("Ignoring test exception: " + ex);
                }
                return ExitCode.Success;
            }

            return await RunVsTestAsync(logger, reportPath, vsTestExePath);
        }

        async Task<ExitCode> RunVsTestAsync(ILogger logger, string vsTestReportDirectoryPath, string vsTestExePath)
        {
            Type testClassAttribute = typeof (TestClassAttribute);
            Type testMethodAttribute = typeof (TestMethodAttribute);

            var directory = new DirectoryInfo(VcsPathHelper.FindVcsRootPath());

            var typesToFind = new List<Type> {testClassAttribute, testMethodAttribute};

            List<string> vsTestConsoleArguments =
                new UnitTestFinder(typesToFind).GetUnitTestFixtureDlls(directory).ToList();

            if (!vsTestConsoleArguments.Any())
            {
                logger.WriteWarning(
                    string.Format("Could not find any VSTest tests in directory '{0}' or any sub-directory",
                        directory.FullName));
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
                Task<ExitCode> execute = ProcessRunner.ExecuteAsync(vsTestExePath, arguments: vsTestConsoleArguments,
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

        void RestoreCurrentDirectory(string currentDirectory)
        {
            Directory.SetCurrentDirectory(currentDirectory);
        }

        string SaveCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        void SetCurrentDirectory(string vsTestReportDirectoryPath)
        {
            Directory.SetCurrentDirectory(vsTestReportDirectoryPath);
        }

        void EnsureTestReportDirectoryExists(string vsTestReportDirectoryPath)
        {
            if (!Directory.Exists(vsTestReportDirectoryPath))
            {
                Directory.CreateDirectory(vsTestReportDirectoryPath);
            }
        }

        static void LogExecution(ILogger logger, IEnumerable<string> arguments, string exePath)
        {
            var args = string.Join(" ", arguments.Select(item => string.Format("\"{0}\"", item)));
            logger.Write(string.Format("Running VSTest {0} {1}", exePath, args));
        }

        static IEnumerable<string> GetVsTestConsoleOptions()
        {
            var options = new List<string> {"/Logger:trx"};
            return options;
        }
    }
}