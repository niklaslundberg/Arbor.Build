using System;

namespace Arbor.X.Core.BuildVariables
{
    [AttributeUsage(AttributeTargets.Field)]
    class VariableDescriptionAttribute : Attribute
    {
        readonly string _description;
        readonly string _defaultValue;
        readonly string _preferUse;

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