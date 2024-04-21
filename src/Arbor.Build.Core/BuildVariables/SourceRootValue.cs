using Zio;

namespace Arbor.Build.Core.BuildVariables;

public class SourceRootValue(DirectoryEntry sourceRoot)
{
    public DirectoryEntry SourceRoot { get; } = sourceRoot;
}