namespace Arbor.X.Core.BuildVariables
{
    public class EnvironmentVariable : IVariable
    {
        public EnvironmentVariable(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; private set; }

        public string Value { get; private set; }

        public override string ToString()
        {
            return this.DisplayValue();
        }
    }
}