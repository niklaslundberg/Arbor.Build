using System;
using System.Diagnostics;

namespace Arbor.X.Core.ProcessUtils
{
    public static class ProcessExtensions
    {
        public static bool IsWin64(this Process process)
        {
            if ((Environment.OSVersion.Version.Major > 5)
                || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
            {
                IntPtr processHandle;
                bool retVal;

                try
                {
                    processHandle = Process.GetProcessById(process.Id).Handle;
                }
                catch
                {
                    return false;
                }

                return NativeMethods.IsWow64Process(processHandle, out retVal) && retVal;
            }

            return false;
        }
    }
}