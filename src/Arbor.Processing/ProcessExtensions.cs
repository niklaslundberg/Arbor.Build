using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Exceptions;
using Arbor.Processing.Core;

namespace Arbor.Processing
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
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }
                    return false;
                }

                return NativeMethods.IsWow64Process(processHandle, out retVal) && retVal;
            }

            return false;
        }

        public static bool IsAlive(
            this Process process,
            Task<ExitCode> task,
            CancellationToken cancellationToken,
            bool done,
            string processWithArgs,
            Action<string, string> toolAction,
            Action<string, string> standardAction,
            Action<string, string> errorAction,
            Action<string, string> verbose)
        {
            if (process == null)
            {
                verbose($"Process '{processWithArgs}' does no longer exist", null);
                return false;
            }

            if (task.IsCompleted || task.IsFaulted || task.IsCanceled)
            {
                TaskStatus status = task.Status;
                verbose($"Task status for process '{processWithArgs}' is {status}", null);
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                verbose($"Cancellation is requested for process '{processWithArgs}'", null);
                return false;
            }

            if (done)
            {
                verbose($"Process '{processWithArgs}' is flagged as done", null);
                return false;
            }

            return true;
        }
    }
}
