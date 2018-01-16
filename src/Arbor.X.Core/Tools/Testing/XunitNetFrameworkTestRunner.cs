using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Properties;
using JetBrains.Annotations;
using Xunit;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(400)]
    [UsedImplicitly]
    public class XunitNetFrameworkTestRunner : ITool
    {
        private string _sourceRoot;

        public async Task<ExitCode> ExecuteAsync([NotNull] ILogger logger, [NotNull] IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (buildVariables == null)
            {
                throw new ArgumentNullException(nameof(buildVariables));
            }

            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetFrameworkEnabled, false);

            if (!enabled)
            {
                logger.WriteDebug("Xunit .NET Framework test runner is not enabled");
                return ExitCode.Success;
            }

            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            IVariable reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();
            string xunitExePath = buildVariables.GetVariableValueOrDefault(WellKnownVariables.XUnitNetFrameworkExePath, Path.Combine(buildVariables.Require(WellKnownVariables.ExternalTools).Value, "xunit", "net452", "xunit.console.exe"));

            Type theoryType = typeof(TheoryAttribute);
            Type factAttribute = typeof(FactAttribute);

            var directory = new DirectoryInfo(_sourceRoot);

            var typesToFind = new List<Type> { theoryType, factAttribute };

            bool runTestsInReleaseConfiguration =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled,
                    true);

            string assemblyFilePrefix = buildVariables.GetVariableValueOrDefault(WellKnownVariables.TestsAssemblyStartsWith, string.Empty);

            List<string> testDlls = new UnitTestFinder(typesToFind)
                .GetUnitTestFixtureDlls(directory, runTestsInReleaseConfiguration, assemblyFilePrefix, FrameworkConstants.NetFramework)
                .ToList();

            if (!testDlls.Any())
            {
                logger.Write("Could not find any DLL files with Xunit test and target framework .NETFramework, skipping Xunit Net Framework tests");
                return ExitCode.Success;
            }

            string xmlReportName = $"{Guid.NewGuid()}.xml";

            var arguments = new List<string>();

            string reportFile = Path.Combine(reportPath.Value, "xunit", xmlReportName);

            var fileInfo = new FileInfo(reportFile);
            fileInfo.Directory.EnsureExists();

            arguments.AddRange(testDlls);
            arguments.Add("-nunit");
            arguments.Add(fileInfo.FullName);

            ExitCode result = await ProcessRunner.ExecuteAsync(
                xunitExePath,
                arguments: arguments,
                standardOutLog: logger.Write,
                standardErrorAction: logger.WriteError,
                toolAction: logger.Write,
                cancellationToken: cancellationToken);

            return result;
        }
    }
}
