using Zio;

namespace Arbor.Build.Core.BuildVariables
{
    public class SourceRootValue
    {
        public SourceRootValue(DirectoryEntry sourceRoot) => SourceRoot = sourceRoot;

        public DirectoryEntry SourceRoot { get; }
    }
}
