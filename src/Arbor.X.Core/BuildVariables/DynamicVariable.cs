using System;

namespace Arbor.X.Core.BuildVariables
{
    public class DynamicVariable : IVariable
    {
        private readonly Func<string> _getValue;
        private readonly string _key;

        public DynamicVariable(string key, Func<string> getValue)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (getValue == null)
            {
                throw new ArgumentNullException(nameof(getValue));
            }

            _key = key;
            _getValue = getValue;
        }

        public string Key => _key;
        public string Value => _getValue();
    }
}