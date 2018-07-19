using System; using Serilog;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Arbor.Processing.Core;

namespace Arbor.X.Core.Tools.Testing
{
    public static class TestReportXslt
    {
        public const string JUnitSuffix = "_junit.xml";

        public static ExitCode Transform(
            FileInfo xmlReport,
            string xsltTemplate,
            ILogger logger)
        {
            Encoding encoding = Encoding.UTF8;

            using (Stream stream = new MemoryStream(encoding.GetBytes(xsltTemplate)))
            {
                using (XmlReader xmlReader = new XmlTextReader(stream))
                {
                    var myXslTransform = new XslCompiledTransform();
                    myXslTransform.Load(xmlReader);

                    logger.Debug("Transforming '{FullName}' to JUnit XML format", xmlReport.FullName);
                    try
                    {
                        TransformReport(xmlReport, JUnitSuffix, encoding, myXslTransform, logger);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Could not transform '{FullName}', {Ex}", xmlReport.FullName);
                        return ExitCode.Failure;
                    }

                    logger.Debug("Successfully transformed '{FullName}' to JUnit XML format", xmlReport.FullName);
                }
            }

            return ExitCode.Success;
        }

        private static void TransformReport(
            FileInfo xmlReport,
            string junitSuffix,
            Encoding encoding,
            XslCompiledTransform myXslTransform,
            ILogger logger)
        {
            // ReSharper disable once PossibleNullReferenceException
            string resultFile = Path.Combine(
                xmlReport.Directory.FullName,
                $"{Path.GetFileNameWithoutExtension(xmlReport.Name)}{junitSuffix}");

            if (File.Exists(resultFile))
            {
                logger.Information("Skipping XML transformation for '{FullName}', the transformation result file '{ResultFile}' already exists", xmlReport.FullName, resultFile);
            }

            using (var fileStream = new FileStream(xmlReport.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var streamReader = new StreamReader(fileStream, encoding))
                {
                    using (XmlReader reportReader = XmlReader.Create(streamReader))
                    {
                        using (var outStream = new FileStream(resultFile, FileMode.Create, FileAccess.Write))
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
