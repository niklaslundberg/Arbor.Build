using System;

namespace Arbor.X.Core.BuildVariables
{
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class VariableDescriptionAttribute : Attribute
    {
        private readonly string _preferUse;

        public VariableDescriptionAttribute(string description, string defaultValue = null, string preferUse = null)
        {
            Description = description;
            DefaultValue = defaultValue;
            _preferUse = preferUse;
        }

        public string Description { get; }

        public string DefaultValue { get; }
    }
}
