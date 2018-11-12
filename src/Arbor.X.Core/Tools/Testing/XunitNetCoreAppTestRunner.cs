using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Properties;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using Arbor.Processing.Core;
using JetBrains.Annotations;
using Serilog;
using Xunit;

namespace Arbor.Build.Core.Tools.Testing
{
    [Priority(400)]
    [UsedImplicitly]
    public class XunitNetCoreAppTestRunner : ITestRunnerTool
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

            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppEnabled, false);

            if (!enabled)
            {
                logger.Information(
                    "Xunit .NET Core App test runner is not enabled, set variable '{XUnitNetCoreAppEnabled}' to true to enable",
                    WellKnownVariables.XUnitNetCoreAppEnabled);

                return ExitCode.Success;
            }

            logger.Information(
                "Xunit .NET Core App test runner is enabled, defined in variable '{Variable}'",
                WellKnownVariables.XUnitNetCoreAppEnabled);

            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            IVariable reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();
            string xunitDllPath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.XUnitNetCoreAppDllPath, null) ??
                Path.Combine(buildVariables.Require(WellKnownVariables.ExternalTools).Value,
                    "xunit",
                    "netcoreapp2.0",
                    "xunit.console.dll");

            if (!File.Exists(xunitDllPath))
            {
                logger.Error("Could not find xunit dll file '{DllFile}'", xunitDllPath);
                return ExitCode.Failure;
            }

            logger.Debug("Using XUnit dll path '{XunitDllPath}'", xunitDllPath);

            Type theoryType = typeof(TheoryAttribute);
            Type factAttribute = typeof(FactAttribute);

            var directory = new DirectoryInfo(_sourceRoot);

            var typesToFind = new List<Type> { theoryType, factAttribute };

            bool? runTestsInReleaseConfiguration =
                buildVariables.GetOptionalBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled);

            string configuration = runTestsInReleaseConfiguration.HasValue
                ? runTestsInReleaseConfiguration.Value ? "release" : "debug"
                : "[ANY]";

            ImmutableArray<string> assemblyFilePrefix = buildVariables.AssemblyFilePrefixes();

            logger.Information(
                "Finding Xunit test DLL files built with '{Configuration}' configuration in directory '{_sourceRoot}'",
                configuration,
                _sourceRoot);
            logger.Information("Looking for types {V} in directory '{_sourceRoot}'",
                string.Join(", ", typesToFind.Select(t => t.FullName)),
                _sourceRoot);

            List<string> testDlls = new UnitTestFinder(typesToFind, logger: logger, debugLogEnabled: true)
                .GetUnitTestFixtureDlls(directory,
                    runTestsInReleaseConfiguration,
                    assemblyFilePrefix,
                    FrameworkConstants.NetCoreApp)
                .ToList();

            if (testDlls.Count == 0)
            {
                logger.Information("Found no .NETCoreApp Assemblies with Xunit tests");
                return ExitCode.Success;
            }

            logger.Debug("Found [{TestDlls}] potential Assembly dll files with tests: {NewLine}: {V}",
                testDlls.Count,
                Environment.NewLine,
                string.Join(Environment.NewLine, testDlls.Select(dll => $" * '{dll}'")));

            string dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty);

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                logger.Information(
                    "Path to 'dotnet.exe' has not been specified, set variable '{DotNetExePath}' or ensure the dotnet.exe is installed in its standard location",
                    WellKnownVariables.DotNetExePath);
                return ExitCode.Failure;
            }

            logger.Debug("Using dotnet.exe in path '{DotNetExePath}'", dotNetExePath);

            string xmlReportName = $"xunit_v2.{Guid.NewGuid()}.xml";

            var arguments = new List<string>();

            string reportFile = Path.Combine(reportPath.Value, "xunit", "v2", xmlReportName);

            var reportFileInfo = new FileInfo(reportFile);
            reportFileInfo.Directory.EnsureExists();

            arguments.Add(xunitDllPath);
            arguments.AddRange(testDlls);

            bool xmlEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppXmlEnabled, true);

            if (xmlEnabled)
            {
                arguments.Add("-xml");
                arguments.Add(reportFileInfo.FullName);
            }

            ExitCode result = await ProcessRunner.ExecuteAsync(
                dotNetExePath,
                arguments: arguments,
                standardOutLog: logger.Information,
                standardErrorAction: logger.Error,
                toolAction: logger.Information,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            ExitCode exitCode = ExitCode.Success;

            if (!result.IsSuccess)
            {
                if (xmlEnabled)
                {
                    bool xunitXmlAnalysisEnabled =
                        buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled,
                            true);

                    if (xunitXmlAnalysisEnabled)
                    {
                        logger.Debug(
                            "Feature flag '{XUnitNetCoreAppXmlAnalysisEnabled}' is enabled and the xunit exit code was {Result}, running xml report to find actual result",
                            WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled,
                            result);

                        exitCode = AnalyzeXml(reportFileInfo, message => logger.Debug(message));
                    }
                }
            }

            if (buildVariables.GetBooleanByKey(
                WellKnownVariables.XUnitNetCoreAppV2XmlXsltToJunitEnabled,
                false))
            {
                logger.Verbose(
                    "Transforming XUnit net core app test reports to JUnit format");

                DirectoryInfo xmlReportDirectory = reportFileInfo.Directory;

                // ReSharper disable once PossibleNullReferenceException
                IReadOnlyCollection<FileInfo> xmlReports = xmlReportDirectory
                    .GetFiles("*.xml")
                    .Where(report => !report.Name.EndsWith(TestReportXslt.JUnitSuffix, StringComparison.Ordinal))
                    .ToReadOnlyCollection();

                if (xmlReports.Count > 0)
                {
                    foreach (FileInfo xmlReport in xmlReports)
                    {
                        logger.Debug("Transforming '{FullName}' to JUnit XML format", xmlReport.FullName);
                        try
                        {
                            ExitCode transformExitCode =
                                TestReportXslt.Transform(xmlReport, XUnitV2JUnitXsl.Xml, logger);

                            if (!transformExitCode.IsSuccess)
                            {
                                return transformExitCode;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Could not transform '{FullName}'", xmlReport.FullName);
                            return ExitCode.Failure;
                        }

                        logger.Debug("Successfully transformed '{FullName}' to JUnit XML format", xmlReport.FullName);
                    }
                }
            }

            return exitCode;
        }

        private static ExitCode AnalyzeXml(FileInfo reportFileInfo, Action<string> logger)
        {
            reportFileInfo.Refresh();

            if (!reportFileInfo.Exists)
            {
                return ExitCode.Failure;
            }

            string fullName = reportFileInfo.FullName;

            using (var fs = new FileStream(fullName, FileMode.Open))
            {
                XDocument xdoc = XDocument.Load(fs);

                XElement[] collections = xdoc.Descendants("assemblies").Descendants("assembly")
                    .Descendants("collection").ToArray();

                int testCount = collections.Count(collection =>
                    int.TryParse(collection.Attribute("total")?.Value, out int total) && total > 0);

                if (testCount == 0)
                {
                    logger?.Invoke($"Found no tests in '{fullName}'");
                    return ExitCode.Failure;
                }

                logger?.Invoke($"Found {testCount} tests in '{fullName}'");

                int failedTests = collections.Count(collection =>
                    int.TryParse(collection.Attribute("failed")?.Value, out int failed) && failed > 0);

                if (failedTests > 0)
                {
                    logger?.Invoke($"Found {failedTests} failing tests in '{fullName}'");
                    return ExitCode.Failure;
                }

                return ExitCode.Success;
            }
        }
    }
}
