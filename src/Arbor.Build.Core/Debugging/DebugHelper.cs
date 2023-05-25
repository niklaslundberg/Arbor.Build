using System.Diagnostics;
using Arbor.Build.Core.BuildVariables;

namespace Arbor.Build.Core.Debugging;

public static class DebugHelper
{
    public static bool IsDebugging(IEnvironmentVariables environmentVariables)
    {
        bool allowDebug = !bool.TryParse(environmentVariables.GetEnvironmentVariable("AllowDebug"),
            out bool allowDebugValue) || allowDebugValue;

        if (!allowDebug)
        {
            return false;
        }

        bool simulateDebug = bool.TryParse(environmentVariables.GetEnvironmentVariable("SimulateDebug"),
            out bool parsed) && parsed;

        return Debugger.IsAttached || simulateDebug;
    }
}