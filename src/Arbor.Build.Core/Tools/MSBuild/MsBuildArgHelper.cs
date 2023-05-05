namespace Arbor.Build.Core.Tools.MSBuild;

public class MsBuildArgHelper
{
    private readonly string _msbuildParameterArgumentDelimiter;

    public MsBuildArgHelper(string msbuildParameterArgumentDelimiter) =>
        _msbuildParameterArgumentDelimiter = msbuildParameterArgumentDelimiter;

    public string FormatPropertyArg(string propertyName, string propertyValue) =>
        FormatArg("property", propertyName, propertyValue);

    public string FormatArg(string arg, string? argSubName = null, string? argValue = null)
    {
        if (string.IsNullOrWhiteSpace(argValue) && string.IsNullOrWhiteSpace(argSubName))
        {
            return _msbuildParameterArgumentDelimiter + arg;
        }

        if (string.IsNullOrWhiteSpace(argValue))
        {
            return $"{_msbuildParameterArgumentDelimiter}{arg}:{argSubName}";
        }

        return $"{_msbuildParameterArgumentDelimiter}{arg}:{argSubName}={argValue}";
    }
}