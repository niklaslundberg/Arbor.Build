using System;

namespace Arbor.X.Core.BuildVariables
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class VariableDescriptionAttribute : Attribute
    {
        private readonly string _description;
        private readonly string _defaultValue;
        private readonly string _preferUse;

        public string DefaultValue
        {
            get { return _defaultValue; }
        }

        public VariableDescriptionAttribute(string description, string defaultValue = null, string preferUse = null)
        {
            _description = description;
            _defaultValue = defaultValue;
            _preferUse = preferUse;
        }

        public string Description
        {
            get { return _description; }
        }
    }
}