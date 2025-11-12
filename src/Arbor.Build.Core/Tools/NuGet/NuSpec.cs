using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Arbor.FS;
using NuGet.Versioning;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet;

public class NuSpec(string packageId, SemanticVersion nuGetPackageVersion, string xml)
{
    public static NuSpec Load(string packageId, SemanticVersion nuGetPackageVersion, FileEntry filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!filePath.Exists)
        {
            throw new ArgumentException($"The file '{filePath}' does not exist", nameof(filePath));
        }

        using var nuspecStream = filePath.Open(FileMode.Open, FileAccess.Read);
        using TextReader reader = new StreamReader(nuspecStream, Encoding.UTF8);
        var xml = XDocument.Load(reader);

        var metaData = xml.Descendants()
            .Where(item => item.Name.LocalName == "package")
            .Descendants()
            .Where(item => item.Name.LocalName == "metadata")
            .ToList();

        metaData.Descendants().Single(item => item.Name.LocalName == "id").Value = packageId;
        metaData.Descendants().Single(item => item.Name.LocalName == "version").Value =
            nuGetPackageVersion.ToNormalizedString();

        string xmlAsString = xml.ToString(SaveOptions.None);

        return new NuSpec(packageId, nuGetPackageVersion, xmlAsString);
    }

    public string PackageId { get; } = packageId;

    public SemanticVersion Version { get; } = nuGetPackageVersion;

    public static NuSpec Parse(FileEntry nuspecFilePath)
    {
        string id;
        SemanticVersion semanticVersion;
        using (var nuspecStream = nuspecFilePath.Open(FileMode.Open, FileAccess.Read))
        {
            using (TextReader reader = new StreamReader(nuspecStream, Encoding.UTF8))
            {
                var document = XDocument.Load(reader);

                var metaData = document.Descendants()
                    .Where(item => item.Name.LocalName == "package")
                    .Descendants()
                    .Where(item => item.Name.LocalName == "metadata")
                    .ToList();

                id = metaData.Descendants().Single(item => item.Name.LocalName == "id").Value;
                string version = metaData.Descendants().Single(item => item.Name.LocalName == "version").Value;

                semanticVersion = SemanticVersion.Parse(version);
            }
        }

        return Load(id, semanticVersion, nuspecFilePath);
    }

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(xml))
        {
            return xml;
        }

        return base.ToString()!;
    }

    public async Task Save(FileEntry filePath) => await filePath.FileSystem.WriteAllTextAsync(filePath.Path, xml, Encoding.UTF8);
}