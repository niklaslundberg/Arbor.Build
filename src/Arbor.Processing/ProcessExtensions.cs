using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Exceptions;
using Arbor.Processing.Core;

namespace Arbor.Processing
{
    public static class ProcessExtensions
    {
        public static bool? IsWin64(this Process process)
        {
            try
            {
                if (process.HasExited)
                {
                    return default;
                }
            }
            catch (Exception)
            {
                return default;
            }

            if ((Environment.OSVersion.Version.Major > 5)
                || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
            {
                IntPtr processHandle;

                try
                {
                    Process processById = Process.GetProcessById(process.Id);

                    processHandle = processById.HasExited ? default : processById.Handle;
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal())
                    {
                        throw;
                    }

                    return default;
                }

                return NativeMethods.IsWow64Process(processHandle, out bool retVal) && retVal;
            }

            return false;
        }

        internal static bool IsAlive(
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

        public static string ExecutablePath(this Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
                var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementBaseObject item in searcher.Get())
                {
                    object idObject = item["ProcessID"];
                    if (!(idObject is int id))
                    {
                        id = int.Parse(idObject.ToString());
                    }

                    object path = item["ExecutablePath"];

                    if (path != null && id == process.Id)
                    {
                        return path.ToString();
                    }
                }
            }

            return string.Empty;
        }

        public static string ToDisplayValue(this Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            return $"{process.Id} {process}";
        }
    }
}
