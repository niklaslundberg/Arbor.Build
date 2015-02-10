using System;

namespace Arbor.X.Core.BuildVariables
{
    public class DynamicVariable : IVariable
    {
        readonly string _key;
        readonly Func<string> _getValue;

        public DynamicVariable(string key, Func<string> getValue)
        {
            _key = key;
            _getValue = getValue;
        }

        public string Key { get { return _key; } }

        public string Value
        {
            get { return _getValue(); }
        }
    }
}