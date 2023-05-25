namespace Arbor.Build.Core.BuildVariables;

public static partial class WellKnownVariables
{
    [VariableDescription("Flag to indicate if Web Deploy packages should be built")]
    public const string NetAssemblyMetadataEnabled = "Arbor.Build.NetAssembly.MetadataEnabled";

    public const string NetAssemblyDescription = "Arbor.Build.NetAssembly.Description";

    public const string NetAssemblyCompany = "Arbor.Build.NetAssembly.Company";

    public const string NetAssemblyCopyright = "Arbor.Build.NetAssembly.Copyright";

    public const string NetAssemblyProduct = "Arbor.Build.NetAssembly.Product";

    public const string NetAssemblyTrademark = "Arbor.Build.NetAssembly.Trademark";
}