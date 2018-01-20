using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Arbor.Defensive.Collections;
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
    public class XunitNetCoreAppTestRunner : ITool
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

            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppEnabled, false);

            if (!enabled)
            {
                logger.WriteDebug("Xunit .NET Core App test runner is not enabled");
                return ExitCode.Success;
            }

            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            IVariable reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();
            string xunitDllPath = buildVariables.GetVariableValueOrDefault(WellKnownVariables.XUnitNetCoreAppDllPath, null) ?? Path.Combine(buildVariables.Require(WellKnownVariables.ExternalTools).Value, "xunit", "netcoreapp2.0", "xunit.console.dll");

            logger.WriteDebug($"Using XUnit dll path '{xunitDllPath}'");

            Type theoryType = typeof(TheoryAttribute);
            Type factAttribute = typeof(FactAttribute);

            var directory = new DirectoryInfo(_sourceRoot);

            var typesToFind = new List<Type> { theoryType, factAttribute };

            bool? runTestsInReleaseConfiguration =
                buildVariables.GetOptionalBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled);

            string configuration = runTestsInReleaseConfiguration.HasValue ? runTestsInReleaseConfiguration.Value ? "release" : "debug" : "[ANY]";

            ImmutableArray<string> assemblyFilePrefix = buildVariables.AssemblyFilePrefixes();

            logger.Write($"Finding Xunit test DLL files built with '{configuration}' configuration in directory '{_sourceRoot}'");
            logger.Write($"Looking for types {string.Join(", ", typesToFind.Select(t => t.FullName))} in directory '{_sourceRoot}'");

            List<string> testDlls = new UnitTestFinder(typesToFind, logger: logger, debugLogEnabled: true)
                .GetUnitTestFixtureDlls(directory, runTestsInReleaseConfiguration, assemblyFilePrefix: assemblyFilePrefix, targetFrameworkPrefix: FrameworkConstants.NetCoreApp)
                .ToList();

            if (!testDlls.Any())
            {
                logger.Write("Found no .NETCoreApp Assemblies with Xunit tests");
                return ExitCode.Success;
            }

            logger.WriteDebug($"Found [{testDlls}] potential Assembly dll files with tests: {Environment.NewLine}: {string.Join(Environment.NewLine, testDlls.Select(dll => $" * '{dll}'"))}");

            string dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty);

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                logger.Write(
                    $"Path to 'dotnet.exe' has not been specified, set variable '{WellKnownVariables.DotNetExePath}' or ensure the dotnet.exe is installed in its standard location");
                return ExitCode.Failure;
            }

            logger.WriteDebug($"Using dotnet.exe in path '{dotNetExePath}'");

            string xmlReportName = $"xunit_v2.{Guid.NewGuid()}.xml";

            var arguments = new List<string>();

            string reportFile = Path.Combine(reportPath.Value, "xunit", "v2", xmlReportName);

            var reportFileInfo = new FileInfo(reportFile);
            reportFileInfo.Directory.EnsureExists();

            arguments.Add(xunitDllPath);
            arguments.AddRange(testDlls);

            bool xmlEnabled =
                buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppXmlEnabled, defaultValue: true);

            if (xmlEnabled)
            {
                arguments.Add("-xml");
                arguments.Add(reportFileInfo.FullName);
            }

            ExitCode result = await ProcessRunner.ExecuteAsync(
                dotNetExePath,
                arguments: arguments,
                standardOutLog: logger.Write,
                standardErrorAction: logger.WriteError,
                toolAction: logger.Write,
                cancellationToken: cancellationToken);

            ExitCode exitCode = ExitCode.Success;

            if (!result.IsSuccess)
            {
                if (xmlEnabled)
                {
                    bool xunitXmlAnalysisEnabled =
                        buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled,
                            defaultValue: true);

                    if (xunitXmlAnalysisEnabled)
                    {
                        logger.WriteDebug($"Feature flag '{WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled}' is enabled and the xunit exit code was {result}, running xml report to find actual result");

                        exitCode = AnalyzeXml(reportFileInfo, message => logger.WriteDebug(message));
                    }
                }
            }

            if (buildVariables.GetBooleanByKey(
                WellKnownVariables.XUnitNetCoreAppV2XmlXsltToJunitEnabled,
                false))
            {
                logger.WriteVerbose(
                    "Transforming XUnit net core app test reports to JUnit format");

                DirectoryInfo xmlReportDirectory = reportFileInfo.Directory;

                // ReSharper disable once PossibleNullReferenceException
                IReadOnlyCollection<FileInfo> xmlReports = xmlReportDirectory
                    .GetFiles("*.xml")
                    .Where(report => !report.Name.EndsWith(TestReportXslt.JUnitSuffix, StringComparison.Ordinal))
                    .ToReadOnlyCollection();

                if (xmlReports.Any())
                {
                    foreach (FileInfo xmlReport in xmlReports)
                    {
                        logger.WriteDebug($"Transforming '{xmlReport.FullName}' to JUnit XML format");
                        try
                        {
                            ExitCode transformExitCode = TestReportXslt.Transform(xmlReport, XUnitV2JUnitXsl.Xml, logger);

                            if (!transformExitCode.IsSuccess)
                            {
                                return transformExitCode;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.WriteError($"Could not transform '{xmlReport.FullName}', {ex}");
                            return ExitCode.Failure;
                        }

                        logger.WriteDebug(
                            $"Successfully transformed '{xmlReport.FullName}' to JUnit XML format");
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

                XElement[] collections = xdoc.Descendants("assemblies").Descendants("assembly").Descendants("collection").ToArray();

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
