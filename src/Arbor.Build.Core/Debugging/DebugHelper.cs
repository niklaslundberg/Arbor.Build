using System;
using System.Diagnostics;
using Arbor.Build.Core.BuildVariables;

namespace Arbor.Build.Core.Debugging
{
    public static class DebugHelper
    {
        public static bool IsDebugging(IEnvironmentVariables environmentVariables) => Debugger.IsAttached
                                          || (bool.TryParse(environmentVariables.GetEnvironmentVariable("SimulateDebug"),
                                                  out bool parsed) && parsed);
    }
}
