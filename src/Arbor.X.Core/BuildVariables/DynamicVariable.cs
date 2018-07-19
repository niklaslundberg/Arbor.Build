using System;

namespace Arbor.X.Core.BuildVariables
{
    public class DynamicVariable : IVariable
    {
        private readonly Func<string> _getValue;

        public DynamicVariable(string key, Func<string> getValue)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
            _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
        }

        public string Key { get; }

        public string Value => _getValue();
    }
}
