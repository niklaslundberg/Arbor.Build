using System;

namespace Arbor.Build.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Flag to indicate if Web Deploy packages should be built")]
        public const string NetAssemblyMetadataEnabled = "Arbor.Build.Build.NetAssembly.MetadataEnabled";

        public const string NetAssemblyDescription = "Arbor.Build.Build.NetAssembly.Description";

        public const string NetAssemblyCompany = "Arbor.Build.Build.NetAssembly.Company";

        [Obsolete]
        public const string NetAssemblyConfiguration = "Arbor.Build.Build.NetAssembly.Configuration";

        public const string NetAssemblyCopyright = "Arbor.Build.Build.NetAssembly.Copyright";

        public const string NetAssemblyProduct = "Arbor.Build.Build.NetAssembly.Product";

        public const string NetAssemblyTrademark = "Arbor.Build.Build.NetAssembly.Trademark";
    }
}
