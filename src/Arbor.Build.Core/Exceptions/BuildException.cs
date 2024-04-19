using System;
using System.Collections.Generic;
using Arbor.Build.Core.BuildVariables;

namespace Arbor.Build.Core.Exceptions;

public sealed class BuildException : Exception
{
    private readonly IReadOnlyCollection<IVariable> _buildVariables;

    public BuildException(string message, IReadOnlyCollection<IVariable> buildVariables)
        : base(message)
    {
        _buildVariables = buildVariables;

        // ReSharper disable once RedundantBaseQualifier
        Data.Add("Arbor.Build.Variables", _buildVariables);
    }

    public BuildException(string message) : base(message) => _buildVariables = [];

    public BuildException(string message, Exception innerException) : base(message, innerException) => _buildVariables = [];

    public override string ToString() => $"{base.ToString()}{Environment.NewLine}Build variables: [{_buildVariables.Count}] {Environment.NewLine}{_buildVariables.Print()}";
}