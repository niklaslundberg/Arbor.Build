using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.ILRepack
{
    // ReSharper disable once InconsistentNaming
    public class ILRepackData
    {
        public ILRepackData(
            string exe,
            IEnumerable<FileInfo> dlls,
            string configuration,
            string platform,
            [NotNull] string targetFramework)
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

            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(targetFramework));
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
            TargetFramework = targetFramework;
        }

        public string Configuration { get; }

        public string Platform { get; }

        public string TargetFramework { get; }

        public string Exe { get; }

        public IEnumerable<FileInfo> Dlls { get; }
    }
}
