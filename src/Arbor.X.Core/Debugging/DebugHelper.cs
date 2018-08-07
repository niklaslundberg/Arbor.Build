using System;
using System.Diagnostics;

namespace Arbor.X.Core
{
    public static class DebugHelper
    {
        public static bool IsDebugging => Debugger.IsAttached ||
                                          bool.TryParse(Environment.GetEnvironmentVariable("SimulateDebug"),
                                              out bool parsed) && parsed;
    }
}
