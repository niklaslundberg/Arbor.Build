using System;

namespace Arbor.Build.Core.BuildVariables
{
    public interface ISpecialFolders
    {
        string GetFolderPath(Environment.SpecialFolder specialFolder);
    }
}