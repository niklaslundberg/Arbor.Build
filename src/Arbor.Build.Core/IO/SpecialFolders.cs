using System;
using Arbor.Build.Core.BuildVariables;

namespace Arbor.Build.Core.IO;

public sealed class SpecialFolders : ISpecialFolders
{
    public static readonly SpecialFolders Default = new();

    public string GetFolderPath(Environment.SpecialFolder specialFolder) =>
        Environment.GetFolderPath(specialFolder);
}