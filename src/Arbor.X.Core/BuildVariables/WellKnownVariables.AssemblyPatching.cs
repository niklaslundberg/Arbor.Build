namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if Web Deploy packages should be built")]
        public static readonly string NetAssemblyMetadataEnabled = Arbor.X.Build + ".NetAssembly.MetadataEnabled";

        public static readonly string NetAssemblyDescription = Arbor.X.Build + ".NetAssembly.Description";

        public static readonly string NetAssemblyCompany = Arbor.X.Build + ".NetAssembly.Company";

        public static readonly string NetAssemblyConfiguration = Arbor.X.Build + ".NetAssembly.Configuration";

        public static readonly string NetAssemblyCopyright = Arbor.X.Build + ".NetAssembly.Copyright";

        public static readonly string NetAssemblyProduct = Arbor.X.Build + ".NetAssembly.Product";

        public static readonly string NetAssemblyTrademark = Arbor.X.Build + ".NetAssembly.Trademark";
    }
}
