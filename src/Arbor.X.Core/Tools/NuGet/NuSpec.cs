using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NuSpec
    {
        readonly string _xml;

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
            var xml = XDocument.Load(filePath);

            var metaData = xml.Descendants()
                .Where(item => item.Name.LocalName == "package").Descendants().Where(item => item.Name.LocalName == "metadata")
                .ToList();

            metaData.Descendants().Single(item => item.Name.LocalName == "id").Value = packageId;
            metaData.Descendants().Single(item => item.Name.LocalName == "version").Value =
                nuGetPackageVersion;
            _xml = xml.ToString(SaveOptions.None);
        }

        public string PackageId { get; private set; }
        public string Version { get; private set; }

        public void Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            File.WriteAllText(filePath, _xml);
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(_xml))
            {
                return _xml;
            }

            return base.ToString();
        }

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

            var document = XDocument.Load(nuspecFilePath);

            var metaData = document.Descendants()
                .Where(item => item.Name.LocalName == "package")
                .Descendants()
                .Where(item => item.Name.LocalName == "metadata")
                .ToList();

            var id = metaData.Descendants().Single(item => item.Name.LocalName == "id").Value;
            var version = metaData.Descendants().Single(item => item.Name.LocalName == "version").Value;

            return new NuSpec(id, version, nuspecFilePath);
        }
    }
}
