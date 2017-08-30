namespace Arbor.X.Core.BuildVariables
{
    public class EnvironmentVariable : IVariable
    {
        public EnvironmentVariable(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }

        public override string ToString()
        {
            return this.DisplayValue();
        }
    }
}
