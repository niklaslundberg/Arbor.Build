using System;
using System.Collections.Generic;
using Arbor.X.Core.BuildVariables;

namespace Arbor.X.Core.Exceptions
{
    public sealed class BuildException : Exception
    {
        private readonly IReadOnlyCollection<IVariable> _buildVariables;

        public BuildException(string message, IReadOnlyCollection<IVariable> buildVariables)
            : base(message)
        {
            _buildVariables = buildVariables;

            // ReSharper disable once RedundantBaseQualifier
            Data.Add("Arbor.X.Variables", _buildVariables);
        }

        public override string ToString()
        {
            return
                $"{base.ToString()}{Environment.NewLine}Build variables: [{_buildVariables.Count}] {Environment.NewLine}{_buildVariables.Print()}";
        }
    }
}
