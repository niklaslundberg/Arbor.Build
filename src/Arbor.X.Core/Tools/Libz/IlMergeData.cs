using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.Libz
{
    // ReSharper disable once InconsistentNaming
    public class IlMergeData
    {
        public IlMergeData(
            string exe,
            IEnumerable<FileInfo> dlls,
            string configuration,
            string platform,
            [CanBeNull] string targetFramework)
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

            if (dllArray.Length == 0)
            {
                throw new ArgumentException("DLL list is empty", nameof(dlls));
            }

            Exe = exe;
            Dlls = dllArray;
            Configuration = configuration;
            Platform = platform;
            TargetFramework = targetFramework;
        }

        public string Configuration { get; }

        public string Platform { get; }

        public string TargetFramework { get; }

        public string Exe { get; }

        public IEnumerable<FileInfo> Dlls { get; }
    }
}
