using System;
using System.Collections.Generic;
using Arbor.X.Core.BuildVariables;

namespace Arbor.X.Core
{
    public class BuildException : Exception
    {
        readonly IReadOnlyCollection<IVariable> _buildVariables;

        public BuildException(string message, IReadOnlyCollection<IVariable> buildVariables) : base(message)
        {
            _buildVariables = buildVariables;
            base.Data.Add("Arbor.X.Variables", _buildVariables);
        }

        public override string ToString()
        {
            return string.Format("{0}{1}Build variables: [{3}] {1}{2}", base.ToString(), Environment.NewLine,
                _buildVariables.Print(), _buildVariables.Count);
        }
    }
}