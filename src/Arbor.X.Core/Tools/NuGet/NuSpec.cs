using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NuSpec
    {
        private readonly string _xml;

        public NuSpec(string packageId, string nuGetPackageVersion, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException($"The file '{filePath}' does not exist", nameof(filePath));
            }

            Version = nuGetPackageVersion;
            PackageId = packageId;
            XDocument xml = XDocument.Load(filePath);

            List<XElement> metaData = xml.Descendants()
                .Where(item => item.Name.LocalName == "package")
                .Descendants()
                .Where(item => item.Name.LocalName == "metadata")
                .ToList();

            metaData.Descendants().Single(item => item.Name.LocalName == "id").Value = packageId;
            metaData.Descendants().Single(item => item.Name.LocalName == "version").Value =
                nuGetPackageVersion;
            _xml = xml.ToString(SaveOptions.None);
        }

        public string PackageId { get; }

        public string Version { get; }

        public static NuSpec Parse(string nuspecFilePath)
        {
            if (string.IsNullOrWhiteSpace(nuspecFilePath))
            {
                throw new ArgumentNullException(nameof(nuspecFilePath));
            }

            if (!File.Exists(nuspecFilePath))
            {
                throw new ArgumentException(
                    $"The file '{nuspecFilePath}' does not exist",
                    nameof(nuspecFilePath));
            }

            XDocument document = XDocument.Load(nuspecFilePath);

            List<XElement> metaData = document.Descendants()
                .Where(item => item.Name.LocalName == "package")
                .Descendants()
                .Where(item => item.Name.LocalName == "metadata")
                .ToList();

            string id = metaData.Descendants().Single(item => item.Name.LocalName == "id").Value;
            string version = metaData.Descendants().Single(item => item.Name.LocalName == "version").Value;

            return new NuSpec(id, version, nuspecFilePath);
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(_xml))
            {
                return _xml;
            }

            return base.ToString();
        }

        public void Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            File.WriteAllText(filePath, _xml);
        }
    }
}
