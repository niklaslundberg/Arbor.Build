using System; using Serilog;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;

using Arbor.X.Core.Properties;
using Machine.Specifications;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(450)]
    public class MSpecTestRunner : ITool
    {
        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            bool enabled = buildVariables.GetBooleanByKey(WellKnownVariables.MSpecEnabled, true);

            if (!enabled)
            {
                logger.Warning("{MachineSpecificationsName} not enabled", MachineSpecificationsConstants.MachineSpecificationsName);
                return ExitCode.Success;
            }

            string externalToolsPath =
                buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            string sourceRoot =
                buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string testReportDirectoryPath =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSpec_ReportPath).ThrowIfEmptyValue().Value;

            string sourceRootOverride =
                buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRootOverride, string.Empty);

            string sourceDirectoryPath;

            if (string.IsNullOrWhiteSpace(sourceRootOverride) || !Directory.Exists(sourceRootOverride))
            {
                sourceDirectoryPath = sourceRoot ?? throw new InvalidOperationException("Source root cannot be null");
            }
            else
            {
                sourceDirectoryPath = sourceRootOverride;
            }

            var directory = new DirectoryInfo(sourceDirectoryPath);
            string mspecExePath = Path.Combine(
                externalToolsPath,
                MachineSpecificationsConstants.MachineSpecificationsName,
                "mspec-clr4.exe");

            bool? runTestsInReleaseConfiguration =
                buildVariables.GetOptionalBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled);

            IEnumerable<Type> typesToFind = new List<Type>
            {
                typeof(It),
                typeof(BehaviorsAttribute),
                typeof(SubjectAttribute),
                typeof(Behaves_like<>)
            };

            logger.Verbose("Scanning directory '{FullName}' for assemblies containing Machine.Specifications tests", directory.FullName);

            ImmutableArray<string> assemblyFilePrefix = buildVariables.AssemblyFilePrefixes();

            if (assemblyFilePrefix.Any())
            {
                logger.Information("Scanning source root '{SourceRoot}' for assemblies using prefix {V}", sourceRoot, string.Join(", ", assemblyFilePrefix.Select(prefix => $"'{prefix}'")));
            }

            List<string> testDlls =
                new UnitTestFinder(typesToFind, logger: logger)
                    .GetUnitTestFixtureDlls(directory,
                        runTestsInReleaseConfiguration,
                        assemblyFilePrefix,
                        FrameworkConstants.NetFramework)
                    .ToList();

            if (!testDlls.Any())
            {
                logger.Warning("No DLL files with {MachineSpecificationsName} specifications was found", MachineSpecificationsConstants.MachineSpecificationsName);
                return ExitCode.Success;
            }

            logger.Debug("Found [{TestDlls}] potential Assembly dll files with tests: {NewLine}: {V}", testDlls, Environment.NewLine, string.Join(Environment.NewLine, testDlls.Select(dll => $" * '{dll}'")));

            var arguments = new List<string>();

            arguments.AddRange(testDlls);

            arguments.Add("--xml");
            string timestamp = DateTime.UtcNow.ToString("O").Replace(":", ".");
            string fileName = "MSpec_" + timestamp + ".xml";
            string xmlReportPath = Path.Combine(testReportDirectoryPath, "Xml", fileName);

            new FileInfo(xmlReportPath).Directory.EnsureExists();

            arguments.Add(xmlReportPath);
            string htmlPath = Path.Combine(testReportDirectoryPath, "Html", "MSpec_" + timestamp);

            new DirectoryInfo(htmlPath).EnsureExists();

            IReadOnlyCollection<string> excludedTags = buildVariables
                .GetVariableValueOrDefault(
                    WellKnownVariables.IgnoredTestCategories,
                    string.Empty)
                .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToReadOnlyCollection();

            arguments.Add("--html");
            arguments.Add(htmlPath);

            bool hasArborTestDll =
                testDlls.Any(dll => dll.IndexOf("arbor", StringComparison.InvariantCultureIgnoreCase) >= 0);

            if (hasArborTestDll || excludedTags.Any())
            {
                var allExcludedTags = new List<string>();

                arguments.Add("--exclude");

                if (hasArborTestDll)
                {
                    allExcludedTags.Add(MSpecInternalConstants.RecursiveArborXTest);
                }

                if (excludedTags.Any())
                {
                    allExcludedTags.AddRange(excludedTags);
                }

                string excludedTagsParameter = string.Join(",", allExcludedTags);

                logger.Information("Running MSpec with excluded tags: {ExcludedTagsParameter}", excludedTagsParameter);

                arguments.Add(excludedTagsParameter);
            }

            // ReSharper disable once CollectionNeverUpdated.Local
            var environmentVariables = new Dictionary<string, string>();

            ExitCode exitCode = await
                ProcessRunner.ExecuteAsync(
                    mspecExePath,
                    arguments: arguments,
                    cancellationToken: cancellationToken,
                    standardOutLog: logger.Information,
                    standardErrorAction: logger.Error,
                    toolAction: logger.Information,
                    verboseAction: logger.Verbose,
                    environmentVariables: environmentVariables,
                    debugAction: logger.Debug);

            if (buildVariables.GetBooleanByKey(
                WellKnownVariables.MSpecJUnitXslTransformationEnabled,
                false))
            {
                logger.Verbose("Transforming {MachineSpecificationsName} test reports to JUnit format", MachineSpecificationsConstants.MachineSpecificationsName);

                DirectoryInfo xmlReportDirectory = new FileInfo(xmlReportPath).Directory;

// ReSharper disable once PossibleNullReferenceException
                IReadOnlyCollection<FileInfo> xmlReports = xmlReportDirectory
                    .GetFiles("*.xml")
                    .Where(report => !report.Name.EndsWith(TestReportXslt.JUnitSuffix, StringComparison.Ordinal))
                    .ToReadOnlyCollection();

                if (xmlReports.Any())
                {
                    foreach (FileInfo xmlReport in xmlReports)
                    {
                        logger.Debug("Transforming '{FullName}' to JUnit XML format", xmlReport.FullName);
                        try
                        {
                            ExitCode transformExitCode = TestReportXslt.Transform(xmlReport, MSpecJUnitXsl.Xml, logger);

                            if (!transformExitCode.IsSuccess)
                            {
                                return transformExitCode;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Could not transform '{FullName}', {Ex}", xmlReport.FullName);
                            return ExitCode.Failure;
                        }

                        logger.Debug("Successfully transformed '{FullName}' to JUnit XML format", xmlReport.FullName);
                    }
                }
            }

            return exitCode;
        }
    }
}
