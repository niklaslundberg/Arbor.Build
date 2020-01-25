namespace Arbor.Build.Core.Tools.MSBuild
{
    public class PackageReferenceElement
    {
        public PackageReferenceElement(string? package, string? version)
        {
            Package = package;
            Version = version;
        }

        public string? Package { get; }

        public string? Version { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Package) && !string.IsNullOrWhiteSpace(Version);

        public override string ToString() => $"{nameof(Package)}: {Package}, {nameof(Version)}: {Version}, {nameof(IsValid)}: {IsValid}";
    }
}
