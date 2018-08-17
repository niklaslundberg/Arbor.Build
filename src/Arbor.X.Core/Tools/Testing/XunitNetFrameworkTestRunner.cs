using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Properties;
using Arbor.Processing;
using Arbor.Processing.Core;
using JetBrains.Annotations;
using Serilog;
using Xunit;

namespace Arbor.Build.Core.Tools.Testing
{
    [Priority(400)]
    [UsedImplicitly]
    public class XunitNetFrameworkTestRunner : ITestRunnerTool
    {
        private string _sourceRoot;

        public async Task<ExitCode> ExecuteAsync(
            [NotNull] ILogger logger,
            [NotNull] IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
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
                logger.Debug("Xunit .NET Framework test runner is not enabled");
                return ExitCode.Success;
            }

            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            IVariable reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();
            string xunitExePath = buildVariables.GetVariableValueOrDefault(WellKnownVariables.XUnitNetFrameworkExePath,
                Path.Combine(buildVariables.Require(WellKnownVariables.ExternalTools).Value,
                    "xunit",
                    "net452",
                    "xunit.console.exe"));

            Type theoryType = typeof(TheoryAttribute);
            Type factAttribute = typeof(FactAttribute);

            var directory = new DirectoryInfo(_sourceRoot);

            var typesToFind = new List<Type> { theoryType, factAttribute };

            bool? runTestsInReleaseConfiguration =
                buildVariables.GetOptionalBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled);

            ImmutableArray<string> assemblyFilePrefix = buildVariables.AssemblyFilePrefixes();

            List<string> testDlls = new UnitTestFinder(typesToFind)
                .GetUnitTestFixtureDlls(directory,
                    runTestsInReleaseConfiguration,
                    assemblyFilePrefix,
                    FrameworkConstants.NetFramework)
                .ToList();

            if (testDlls.Count == 0)
            {
                logger.Information(
                    "Could not find any DLL files with Xunit test and target framework .NETFramework, skipping Xunit Net Framework tests");
                return ExitCode.Success;
            }

            logger.Debug("Found [{TestDlls}] potential Assembly dll files with tests: {NewLine}: {V}",
                testDlls,
                Environment.NewLine,
                string.Join(Environment.NewLine, testDlls.Select(dll => $" * '{dll}'")));

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
                standardOutLog: logger.Information,
                standardErrorAction: logger.Error,
                toolAction: logger.Information,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
