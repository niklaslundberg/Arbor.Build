using System;

namespace Arbor.Build.Core.BuildVariables
{
    public sealed class DynamicVariable : IVariable
    {
        public DynamicVariable(string key, string? initialValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            Key = key;
            Value = initialValue;
        }

        public string Key { get; }

        public string? Value { get; set; }
    }
}
