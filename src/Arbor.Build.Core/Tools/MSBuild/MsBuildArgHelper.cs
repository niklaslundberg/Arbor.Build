namespace Arbor.Build.Core.Tools.MSBuild;

public class MsBuildArgHelper(string msbuildParameterArgumentDelimiter)
{
    public string FormatPropertyArg(string propertyName, string propertyValue) =>
        FormatArg("property", propertyName, propertyValue);

    public string FormatArg(string arg, string? argSubName = null, string? argValue = null)
    {
        if (string.IsNullOrWhiteSpace(argValue) && string.IsNullOrWhiteSpace(argSubName))
        {
            return msbuildParameterArgumentDelimiter + arg;
        }

        if (string.IsNullOrWhiteSpace(argValue))
        {
            return $"{msbuildParameterArgumentDelimiter}{arg}:{argSubName}";
        }

        return $"{msbuildParameterArgumentDelimiter}{arg}:{argSubName}={argValue}";
    }
}