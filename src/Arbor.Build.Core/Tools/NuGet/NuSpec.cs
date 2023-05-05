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

public class NuSpec
{
    private readonly string _xml;

    public NuSpec(string packageId, SemanticVersion nuGetPackageVersion, FileEntry filePath)
    {
        if (filePath is null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!filePath.Exists)
        {
            throw new ArgumentException($"The file '{filePath}' does not exist", nameof(filePath));
        }

        Version = nuGetPackageVersion;
        PackageId = packageId;

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

        _xml = xml.ToString(SaveOptions.None);
    }

    public string PackageId { get; }

    public SemanticVersion Version { get; }

    public static NuSpec Parse(FileEntry nuspecFilePath)
    {
        string id;
        string version;
        using (var nuspecStream = nuspecFilePath.Open(FileMode.Open, FileAccess.Read))
        {
            using TextReader reader = new StreamReader(nuspecStream, Encoding.UTF8);
            var document = XDocument.Load(reader);

            var metaData = document.Descendants()
                .Where(item => item.Name.LocalName == "package")
                .Descendants()
                .Where(item => item.Name.LocalName == "metadata")
                .ToList();

            id = metaData.Descendants().Single(item => item.Name.LocalName == "id").Value;
            version = metaData.Descendants().Single(item => item.Name.LocalName == "version").Value;
        }

        var semanticVersion = SemanticVersion.Parse(version);

        return new NuSpec(id, semanticVersion, nuspecFilePath);
    }

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(_xml))
        {
            return _xml;
        }

        return base.ToString()!;
    }

    public async Task Save(FileEntry filePath) => await filePath.FileSystem.WriteAllTextAsync(filePath.Path, _xml, Encoding.UTF8);
}