using System;
using System.Collections.Generic;

using Arbor.X.Core.BuildVariables;

namespace Arbor.X.Core.Exceptions
{
    public class BuildException : Exception
    {
        readonly IReadOnlyCollection<IVariable> _buildVariables;

        public BuildException(string message, IReadOnlyCollection<IVariable> buildVariables)
            : base(message)
        {
            _buildVariables = buildVariables;
            base.Data.Add("Arbor.X.Variables", _buildVariables);
        }

        public override string ToString()
        {
            return $"{base.ToString()}{Environment.NewLine}Build variables: [{_buildVariables.Count}] {Environment.NewLine}{_buildVariables.Print()}";
        }
    }
}
