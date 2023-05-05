using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Testing;

public static class TestReportXslt
{
    public const string JUnitSuffix = "_junit.xml";

    public static ExitCode Transform(
        [NotNull] FileEntry xmlReport,
        [NotNull] string xsltTemplate,
        [NotNull] ILogger logger,
        bool deleteOriginal = true)
    {
        if (xmlReport is null)
        {
            throw new ArgumentNullException(nameof(xmlReport));
        }

        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (!xmlReport.Exists)
        {
            throw new InvalidOperationException($"The report file '{xmlReport}' does not exist");
        }

        if (string.IsNullOrWhiteSpace(xsltTemplate))
        {
            throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(xsltTemplate));
        }

        Encoding encoding = Encoding.UTF8;

        using (Stream stream = new MemoryStream(encoding.GetBytes(xsltTemplate)))
        {
            using XmlReader xmlReader = new XmlTextReader(stream);
            var myXslTransform = new XslCompiledTransform();
            myXslTransform.Load(xmlReader);

            logger.Debug("Transforming '{FullName}' to JUnit XML format", xmlReport.FullName);
            try
            {
                TransformReport(xmlReport, JUnitSuffix, encoding, myXslTransform, logger, deleteOriginal);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Could not transform '{FullName}'", xmlReport.FullName);
                return ExitCode.Failure;
            }

            logger.Debug("Successfully transformed '{FullName}' to JUnit XML format", xmlReport.FullName);
        }

        return ExitCode.Success;
    }

    private static void TransformReport(
        FileEntry xmlReport,
        string junitSuffix,
        Encoding encoding,
        XslCompiledTransform myXslTransform,
        ILogger logger, bool deleteOriginal)
    {
        // ReSharper disable once PossibleNullReferenceException
        var resultFile = UPath.Combine(
            xmlReport.Directory.Path,
            $"{Path.GetFileNameWithoutExtension(xmlReport.Name)}{junitSuffix}");

        if (xmlReport.FileSystem.FileExists(resultFile))
        {
            logger.Information(
                "Skipping XML transformation for '{FullName}', the transformation result file '{ResultFile}' already exists",
                xmlReport.FullName,
                resultFile);
        }

        using (var fileStream = xmlReport.Open( FileMode.Open, FileAccess.Read))
        {
            using var streamReader = new StreamReader(fileStream, encoding);
            using var reportReader = XmlReader.Create(streamReader);
            using var outStream = xmlReport.FileSystem.OpenFile(resultFile, FileMode.Create, FileAccess.Write);
            using XmlWriter reportWriter = new XmlTextWriter(outStream, encoding);
            myXslTransform.Transform(reportReader, reportWriter);
        }

        if (deleteOriginal)
        {
            xmlReport.DeleteIfExists();
        }
    }
}