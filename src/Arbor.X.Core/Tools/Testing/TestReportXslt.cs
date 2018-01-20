using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Arbor.Processing.Core;
using Arbor.X.Core.Logging;

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

                    logger.WriteDebug($"Transforming '{xmlReport.FullName}' to JUnit XML format");
                    try
                    {
                        TransformReport(xmlReport, JUnitSuffix, encoding, myXslTransform, logger);
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

            return ExitCode.Success;
        }

        static void TransformReport(
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
                logger.Write(
                    $"Skipping XML transformation for '{xmlReport.FullName}', the transformation result file '{resultFile}' already exists");
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
