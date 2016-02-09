using System;
using System.Collections.Generic;
using System.Linq;
using Alphaleonis.Win32.Filesystem;

namespace Arbor.X.Core.Tools.ILRepack
{
    // ReSharper disable once InconsistentNaming
    public class ILRepackData
    {
        public ILRepackData(string exe, IEnumerable<FileInfo> dlls, string configuration, string platform)
        {
            if (string.IsNullOrWhiteSpace(exe))
            {
                throw new ArgumentNullException(nameof(exe));
            }

            if (dlls == null)
            {
                throw new ArgumentNullException(nameof(dlls));
            }

            if (string.IsNullOrWhiteSpace(configuration))
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(platform))
            {
                throw new ArgumentNullException(nameof(platform));
            }

            FileInfo[] dllArray = dlls.ToArray();

            if (!dllArray.Any())
            {
                throw new ArgumentException("DLL list is empty", nameof(dlls));
            }

            Exe = exe;
            Dlls = dllArray;
            Configuration = configuration;
            Platform = platform;
        }

        public string Configuration { get; }

        public string Platform { get; }

        public string Exe { get; }

        public IEnumerable<FileInfo> Dlls { get; }
    }
}
