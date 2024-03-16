namespace Arbor.Build.Core.Tools.MSBuild;

public class PackageReferenceElement(string? package, string? version)
{
    public string? Package { get; } = package;

    public string? Version { get; } = version;

    public bool IsValid => !string.IsNullOrWhiteSpace(Package) && !string.IsNullOrWhiteSpace(Version);

    public override string ToString() => $"{nameof(Package)}: {Package}, {nameof(Version)}: {Version}, {nameof(IsValid)}: {IsValid}";
}