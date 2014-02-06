using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arbor.Aesculus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(450)]
    public class VSTestRunner : ITool
    {
        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables)
        {
            var reportPath =
                buildVariables.Require(WellKnownVariables.ExternalTools_VSTest_ReportPath).ThrowIfEmptyValue().Value;

            if (!buildVariables.Any(bv => bv.Key == WellKnownVariables.ExternalTools_VSTest_ExePath) || string.IsNullOrWhiteSpace(buildVariables.Single(key => key.Key == WellKnownVariables.ExternalTools_VSTest_ExePath).Value))
            {
                logger.WriteWarning(WellKnownVariables.ExternalTools_VSTest_ExePath + " is not defined, cannot run any VSTests");
                return ExitCode.Success;
            }

            var vsTestExePath =
                buildVariables.Require(WellKnownVariables.ExternalTools_VSTest_ExePath).ThrowIfEmptyValue().Value;

            var ignoreTestFailuresVariable =
                buildVariables.SingleOrDefault(key => key.Key == WellKnownVariables.IgnoreTestFailures);

            bool ignoreTestFailures;

            if (ignoreTestFailuresVariable != null &&
                bool.TryParse(ignoreTestFailuresVariable.Value, out ignoreTestFailures))
            {
                try
                {
                    return await RunVSTestAsync(logger, reportPath, vsTestExePath);
                }
                catch (Exception ex)
                {
                    logger.WriteWarning("Ignoring test exception: " + ex.ToString());
                }
                return ExitCode.Success;
            }

            return await RunVSTestAsync(logger, reportPath, vsTestExePath);
        }

        async Task<ExitCode> RunVSTestAsync(ILogger logger, string msTestReportDirectoryPath, string msTestExePath)
        {
            Type testClassAttribute = typeof (TestClassAttribute);
            Type testMethodAttribute = typeof (TestMethodAttribute);

            var directory = new DirectoryInfo(VcsPathHelper.FindVcsRootPath());

            var typesToFind = new List<Type> {testClassAttribute, testMethodAttribute};

            List<string> msTestConsoleArguments =
                new UnitTestFinder(typesToFind).GetUnitTestFixtureDlls(directory).ToList();

            if (!msTestConsoleArguments.Any())
            {
                logger.WriteWarning(
                    string.Format("Could not find any VSTest tests in directory '{0}' or any sub-directory",
                        directory.FullName));
                return ExitCode.Success;
            }

            IEnumerable<string> options = GetMSTestConsoleOptions();

            msTestConsoleArguments.AddRange(options);

            EnsureTestReportDirectoryExists(msTestReportDirectoryPath);

            var oldCurrentDirectory = SaveCurrentDirectory();

            SetCurrentDirectory(msTestReportDirectoryPath);

            LogExecution(logger, msTestConsoleArguments, msTestExePath);

            try
            {
                Task<ExitCode> execute = ProcessRunner.ExecuteAsync(msTestExePath, arguments: msTestConsoleArguments,
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

        static IEnumerable<string> GetMSTestConsoleOptions()
        {
            var options = new List<string> {"/Logger:trx"};
            return options;
        }
    }
}