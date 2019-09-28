using System;
using System.Diagnostics;
using System.Globalization;
using System.Management;

namespace Arbor.Processing
{
    public static class ProcessExtensions
    {

        public static string ExecutablePath(this Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            try
            {
                return process.MainModule?.FileName;
            }
            catch
            {
                const string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
                using var searcher = new ManagementObjectSearcher(query);
                foreach (ManagementBaseObject item in searcher.Get())
                {
                    object idObject = item["ProcessID"];
                    if (!(idObject is int id))
                    {
                        id = int.Parse(idObject.ToString(), CultureInfo.InvariantCulture);
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
