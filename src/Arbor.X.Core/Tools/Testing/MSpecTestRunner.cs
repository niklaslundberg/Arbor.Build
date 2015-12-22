using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using Alphaleonis.Win32.Filesystem;

using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;
using Machine.Specifications;
using FileAccess = System.IO.FileAccess;
using FileMode = System.IO.FileMode;
using FileStream = System.IO.FileStream;
using MemoryStream = System.IO.MemoryStream;
using Stream = System.IO.Stream;
using StreamReader = System.IO.StreamReader;

namespace Arbor.X.Core.Tools.Testing
{
    [Priority(450)]
    public class MSpecTestRunner : ITool
    {
        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            var enabled = buildVariables.GetBooleanByKey(WellKnownVariables.MSpecEnabled, defaultValue: true);

            if (!enabled)
            {
                logger.WriteWarning($"{nameof(Machine.Specifications)} not enabled");
                return ExitCode.Success;
            }

            string externalToolsPath =
                buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            string sourceRoot =
                buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string testReportDirectoryPath =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSpec_ReportPath).ThrowIfEmptyValue().Value;

            string sourceRootOverride = buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRootOverride, "");

            string sourceDirectoryPath;

            if (string.IsNullOrWhiteSpace(sourceRootOverride) || !Directory.Exists(sourceRootOverride))
            {
                if (sourceRoot == null)
                {
                    throw new InvalidOperationException("Source root cannot be null");
                }

                sourceDirectoryPath = sourceRoot;

            }
            else
            {
                sourceDirectoryPath = sourceRootOverride;
            }

            var directory = new DirectoryInfo(sourceDirectoryPath);
            string mspecExePath = Path.Combine(externalToolsPath, nameof(Machine.Specifications), "mspec-clr4.exe");

            bool runTestsInReleaseConfiguration =
                buildVariables.GetBooleanByKey(
                    WellKnownVariables.RunTestsInReleaseConfigurationEnabled,
                    defaultValue: true);

            IEnumerable<Type> typesToFind = new List<Type>
                                            {
                                                typeof (It),
                                                typeof (BehaviorsAttribute),
                                                typeof (SubjectAttribute),
                                                typeof (Behaves_like<>),
                                            };
            List<string> testDlls =
                new UnitTestFinder(typesToFind, logger: logger).GetUnitTestFixtureDlls(directory, runTestsInReleaseConfiguration).ToList();

            if (!testDlls.Any())
            {
                logger.WriteWarning($"No DLL files with {nameof(Machine.Specifications)} specifications was found");
                return ExitCode.Success;
            }

            var arguments = new List<string>();

            arguments.AddRange(testDlls);

            arguments.Add("--xml");
            var timestamp = DateTime.UtcNow.ToString("O").Replace(":", ".");
            var fileName = "MSpec_" + timestamp + ".xml";
            var xmlReportPath = Path.Combine(testReportDirectoryPath, "Xml", fileName);

            new FileInfo(xmlReportPath).Directory.EnsureExists();

            arguments.Add(xmlReportPath);
            var htmlPath = Path.Combine(testReportDirectoryPath, "Html", "MSpec_" + timestamp);

            new DirectoryInfo(htmlPath).EnsureExists();

            var excludedTags = buildVariables.GetVariableValueOrDefault(WellKnownVariables.IgnoredTestCategories,
                defaultValue: "")
                .Split(new[]{","}, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToReadOnlyCollection();

            arguments.Add("--html");
            arguments.Add(htmlPath);

            bool hasArborTestDll = testDlls.Any(dll => dll.IndexOf("arbor", StringComparison.InvariantCultureIgnoreCase) >= 0);

            if (hasArborTestDll || excludedTags.Any())
            {
                List<string> allExcludedTags = new List<string>();

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

                logger.Write($"Running MSpec with excluded tags: {excludedTagsParameter}");

                arguments.Add(excludedTagsParameter);
            }

            // ReSharper disable once CollectionNeverUpdated.Local
            var environmentVariables = new Dictionary<string, string>();

            var exitCode = await
                ProcessRunner.ExecuteAsync(mspecExePath, arguments: arguments, cancellationToken: cancellationToken,
                    standardOutLog: logger.Write, standardErrorAction: logger.WriteError, toolAction: logger.Write,
                    verboseAction: logger.WriteVerbose, environmentVariables: environmentVariables, debugAction: logger.WriteDebug);

            if (buildVariables.GetBooleanByKey(WellKnownVariables.MSpecJUnitXslTransformationEnabled,
                defaultValue: false))
            {
                logger.WriteVerbose($"Transforming {nameof(Machine.Specifications)} test reports to JUnit format");

                const string JunitSuffix = "_junit.xml";

                var xmlReportDirectory = new FileInfo(xmlReportPath).Directory;
// ReSharper disable once PossibleNullReferenceException
                var xmlReports = xmlReportDirectory
                    .GetFiles("*.xml")
                    .Where(report => !report.Name.EndsWith(JunitSuffix))
                    .ToReadOnlyCollection();

                if (xmlReports.Any())
                {
                    Encoding encoding = Encoding.UTF8;
                    using (Stream stream = new MemoryStream(encoding.GetBytes(MSpecJUnitXsl.Xml)))
                    {
                        using (XmlReader xmlReader = new XmlTextReader(stream))
                        {
                            XslCompiledTransform myXslTransform = new XslCompiledTransform();
                            myXslTransform.Load(xmlReader);

                            foreach (var xmlReport in xmlReports)
                            {
                                logger.WriteDebug($"Transforming '{xmlReport.FullName}' to JUnit XML format");
                                try
                                {
                                    TransformReport(xmlReport, JunitSuffix, encoding, myXslTransform, logger);
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
                }
            }
            return exitCode;
        }

        static void TransformReport(FileInfo xmlReport, string junitSuffix, Encoding encoding, XslCompiledTransform myXslTransform, ILogger logger)
        {
            // ReSharper disable once PossibleNullReferenceException
            string resultFile = Path.Combine(xmlReport.Directory.FullName,
                $"{Path.GetFileNameWithoutExtension(xmlReport.Name)}{junitSuffix}");

            if (File.Exists(resultFile))
            {
                logger.Write(
                    $"Skipping XML transformation for '{xmlReport.FullName}', the transformation result file '{resultFile}' already exists");
                return;
            }

            using (FileStream fileStream = new FileStream(xmlReport.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var streamReader = new StreamReader(fileStream, encoding))
                {
                    using (XmlReader reportReader = XmlReader.Create(streamReader))
                    {
                        using (FileStream outStream = new FileStream(resultFile, FileMode.Create, FileAccess.Write))
                        {
                            using (XmlWriter reportWriter = new XmlTextWriter(outStream, encoding))
                            {
                                myXslTransform.Transform(reportReader, reportWriter);
                            }
                        }
                    }
                }
            }

            File.Delete(xmlReport.FullName);
        }
    }
}
