using System;
using System.Collections.Generic;
using Zio;

namespace Arbor.Build.Core.Tools.MSBuild;

public class BuildContext(IFileSystem fileSystem)
{
    private DirectoryEntry? _sourceRoot;

    public IFileSystem FileSystem { get; } = fileSystem;

    public BuildConfiguration? CurrentBuildConfiguration { get; set; }

    public HashSet<string> Configurations { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasSourceRootSet => _sourceRoot is { };

    public DirectoryEntry SourceRoot
    {
        get
        {
            if (_sourceRoot is null)
            {
                throw new InvalidOperationException("Source root is not set in build context");
            }

            return _sourceRoot;
        }
        set
        {
            if (_sourceRoot is {})
            {
                throw new InvalidOperationException("Source root has already been initialized");
            }

            _sourceRoot = value;
        }
    }
}