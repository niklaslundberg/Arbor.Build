using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using Alphaleonis.Win32.Filesystem;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Extensions;
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
                logger.WriteWarning("Machine.Specifications not enabled");
                return ExitCode.Success;
            }

            string externalToolsPath =
                buildVariables.Require(WellKnownVariables.ExternalTools).ThrowIfEmptyValue().Value;

            string sourceRoot =
                buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string testReportDirectoryPath =
                buildVariables.Require(WellKnownVariables.ExternalTools_MSpec_ReportPath).ThrowIfEmptyValue().Value;

            var sourceRootOverride = buildVariables.GetVariableValueOrDefault(WellKnownVariables.SourceRootOverride, "");

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
            string mspecExePath = Path.Combine(externalToolsPath, "Machine.Specifications", "mspec-clr4.exe");


            IEnumerable<Type> typesToFind = new List<Type>
                                            {
                                                typeof (It),
                                                typeof (BehaviorsAttribute),
                                                typeof (SubjectAttribute),
                                                typeof (Behaves_like<>),
                                            };
            List<string> testDlls =
                new UnitTestFinder(typesToFind, logger: logger).GetUnitTestFixtureDlls(directory).ToList();

            if (!testDlls.Any())
            {
                logger.WriteWarning("No DLL files with Machine.Specifications specifications was found");
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

                logger.Write(string.Format("Running MSpec with excluded tags: {0}", excludedTagsParameter));

                arguments.Add(excludedTagsParameter);
            }

            var environmentVariables = new Dictionary<string, string>();
            
            var exitCode = await
                ProcessRunner.ExecuteAsync(mspecExePath, arguments: arguments, cancellationToken: cancellationToken,
                    standardOutLog: logger.Write, standardErrorAction: logger.WriteError, toolAction: logger.Write,
                    verboseAction: logger.WriteVerbose, environmentVariables: environmentVariables, debugAction: logger.WriteDebug);

            if (buildVariables.GetBooleanByKey(WellKnownVariables.MSpecJUnitXslTransformationEnabled,
                defaultValue: false))
            {
                logger.WriteVerbose("Transforming Machine.Specifications test reports to JUnit format");

                const string junitSuffix = "_junit.xml";

                var xmlReportDirectory = new FileInfo(xmlReportPath).Directory;
// ReSharper disable once PossibleNullReferenceException
                var xmlReports = xmlReportDirectory
                    .GetFiles("*.xml")
                    .Where(report => !report.Name.EndsWith(junitSuffix))
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
                                logger.WriteDebug(string.Format("Transforming '{0}' to JUnit XML format", xmlReport.FullName));
                                try
                                {
                                    TransformReport(xmlReport, junitSuffix, encoding, myXslTransform, logger);
                                }
                                catch (Exception ex)
                                {
                                    logger.WriteError(string.Format("Could not transform '{0}', {1}", xmlReport.FullName, ex));
                                    return ExitCode.Failure;
                                }
                                logger.WriteDebug(string.Format("Successfully transformed '{0}' to JUnit XML format", xmlReport.FullName));
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
                Path.GetFileNameWithoutExtension(xmlReport.Name) + junitSuffix);

            if (File.Exists(resultFile))
            {
                logger.Write(string.Format("Skipping XML transformation for '{0}', the transformation result file '{1}' already exists", xmlReport.FullName, resultFile));
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