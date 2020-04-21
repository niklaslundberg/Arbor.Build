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
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.Testing
{
    [Priority(400)]
    [UsedImplicitly]
    public class DotNetTestRunner : ITestRunnerTool
    {
        public DotNetTestRunner(BuildContext buildContext) => _buildContext = buildContext;

        private const string AnyConfiguration = "[Any]";
        private readonly BuildContext _buildContext;
        private string _sourceRoot;

        public async Task<ExitCode> ExecuteAsync(
            [NotNull] ILogger logger,
            [NotNull] IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
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

            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppV2Enabled, true);

            if (!enabled)
            {
                logger.Information(
                    ".NET test runner is not enabled, set variable '{XUnitNetCoreAppV2Enabled}' to true to enable",
                    WellKnownVariables.XUnitNetCoreAppV2Enabled);

                return ExitCode.Success;
            }

            logger.Information(
                ".NET test runner is enabled, defined in key {Key}",
                WellKnownVariables.XUnitNetCoreAppV2Enabled);

            _sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;
            IVariable reportPath = buildVariables.Require(WellKnownVariables.ReportPath).ThrowIfEmptyValue();

            bool? runTestsInReleaseConfiguration =
                buildVariables.GetOptionalBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled);

            bool runTestsInAnyConfiguration =
                buildVariables.GetBooleanByKey(WellKnownVariables.RunTestsInAnyConfigurationEnabled);

            string configuration;

            if (runTestsInAnyConfiguration)
            {
                configuration = "[ANY]";
            }
            else if (runTestsInReleaseConfiguration == true)
            {
                configuration = WellKnownConfigurations.Release;
            } else if (_buildContext.Configurations.Count == 1)
            {
                configuration = _buildContext.Configurations.Single();
            }
            else
            {
                configuration = WellKnownConfigurations.Debug;
            }

            ImmutableArray<string> assemblyFilePrefix = buildVariables.AssemblyFilePrefixes();

            string? dotNetExePath =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath, string.Empty);

            if (string.IsNullOrWhiteSpace(dotNetExePath))
            {
                logger.Error(
                    "Path to 'dotnet.exe' has not been specified, set variable '{DotNetExePath}' or ensure the dotnet.exe is installed in its standard location",
                    WellKnownVariables.DotNetExePath);

                return ExitCode.Failure;
            }

            logger.Debug("Using dotnet.exe in path '{DotNetExePath}'", dotNetExePath);

            var testDirectories = new DirectoryInfo(_sourceRoot)
                .GetFiles("*test*.csproj", SearchOption.AllDirectories)
                .Where(file => file?.Directory != null && (assemblyFilePrefix.Length == 0 || assemblyFilePrefix.Any(prefix => file.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
                .Select(file => file.Directory.FullName)
                .ToHashSet();

            ExitCode exitCode = ExitCode.Success;

            foreach (string testDirectory in testDirectories)
            {
                var directoryInfo = new DirectoryInfo(testDirectory);
                string xmlReportName = $"dotnet.{directoryInfo.Name}.trx";

                var arguments = new List<string> {"test", testDirectory};

                if (!configuration.Equals(AnyConfiguration, StringComparison.OrdinalIgnoreCase))
                {
                    arguments.Add("--no-build");
                    arguments.Add("--configuration");
                    arguments.Add(configuration);
                }

                bool xmlEnabled =
                    buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppXmlEnabled, true);

                string reportFile = Path.Combine(reportPath.Value, "dotnet", xmlReportName);

                var reportFileInfo = new FileInfo(reportFile);
                reportFileInfo.Directory.EnsureExists();

                if (xmlEnabled)
                {
                    arguments.Add($"--logger:trx;LogFileName={reportFileInfo.FullName}");
                }

                ExitCode result = await ProcessRunner.ExecuteProcessAsync(
                    dotNetExePath,
                    arguments,
                    logger.Information,
                    logger.Error,
                    logger.Information,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    exitCode = result;

                    if (xmlEnabled)
                    {
                        bool xmlAnalysisEnabled =
                            buildVariables.GetBooleanByKey(WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled,
                                true);

                        if (xmlAnalysisEnabled)
                        {
                            logger.Debug(
                                "Feature flag '{XUnitNetCoreAppXmlAnalysisEnabled}' is enabled and the xunit exit code was {Result}, running xml report to find actual result",
                                WellKnownVariables.XUnitNetCoreAppXmlAnalysisEnabled,
                                result);

                            ExitCode projectExitCode = AnalyzeXml(reportFileInfo,
                                message => logger.Debug("{Message}", message));

                            if (!projectExitCode.IsSuccess)
                            {
                                exitCode = projectExitCode;
                            }
                        }
                    }
                }

                if (buildVariables.GetBooleanByKey(
                    WellKnownVariables.XUnitNetCoreAppV2XmlXsltToJunitEnabled))
                {
                    logger.Verbose(
                        "Transforming TRX test reports to JUnit format");

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
                                    TestReportXslt.Transform(xmlReport, Trx2UnitXsl.Xml, logger);

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

                            logger.Debug("Successfully transformed '{FullName}' to JUnit XML format",
                                xmlReport.FullName);
                        }
                    }
                }

                if (buildVariables.GetBooleanByKey(
                    WellKnownVariables.XUnitNetCoreAppV2TrxXsltToJunitEnabled))
                {
                    logger.Verbose(
                        "Transforming TRX test reports to JUnit format");

                    DirectoryInfo xmlReportDirectory = reportFileInfo.Directory;

                    // ReSharper disable once PossibleNullReferenceException
                    IReadOnlyCollection<FileInfo> xmlReports = xmlReportDirectory
                        .GetFiles("*.trx")
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
                                    TestReportXslt.Transform(xmlReport, Trx2UnitXsl.TrxTemplate, logger);

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

                            logger.Debug("Successfully transformed '{FullName}' to JUnit XML format",
                                xmlReport.FullName);
                        }
                    }
                }
                else
                {
                    logger.Verbose(
                        "TRX transformation to JUnit format is disabled, defined in key '{Key}' and '{TrxKey}'",
                        WellKnownVariables.XUnitNetCoreAppV2XmlXsltToJunitEnabled,
                        WellKnownVariables.XUnitNetCoreAppV2TrxXsltToJunitEnabled);
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

            using var fs = new FileStream(fullName, FileMode.Open);
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