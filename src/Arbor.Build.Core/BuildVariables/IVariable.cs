namespace Arbor.Build.Core.BuildVariables
{
    public interface IVariable
    {
        string Key { get; }

        string? Value { get; }
    }
}
