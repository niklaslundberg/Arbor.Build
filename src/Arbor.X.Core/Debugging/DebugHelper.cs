using System;
using System.Diagnostics;

namespace Arbor.Build.Core.Debugging
{
    public static class DebugHelper
    {
        public static bool IsDebugging => Debugger.IsAttached ||
                                          bool.TryParse(Environment.GetEnvironmentVariable("SimulateDebug"),
                                              out bool parsed) && parsed;
    }
}
