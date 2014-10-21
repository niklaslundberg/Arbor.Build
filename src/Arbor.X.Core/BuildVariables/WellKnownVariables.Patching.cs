namespace Arbor.X.Core.BuildVariables
{
    public static partial class WellKnownVariables
    {
        [VariableDescription("Assembly version patching file pattern")]
        public static readonly string AssemblyFilePatchingFilePattern = Arbor.X.Build + ".NetAssembly.Patching.FilePattern";
    }
}