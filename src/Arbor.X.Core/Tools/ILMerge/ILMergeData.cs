using System;
using System.Collections.Generic;
using System.Linq;
using Alphaleonis.Win32.Filesystem;

namespace Arbor.X.Core.Tools.ILMerge
{
    public class ILMergeData
    {
        readonly string _configuration;
        readonly IEnumerable<FileInfo> _dlls;
        readonly string _exe;
        readonly string _platform;

        public ILMergeData(string exe, IEnumerable<FileInfo> dlls, string configuration, string platform)
        {
            if (string.IsNullOrWhiteSpace(exe))
            {
                throw new ArgumentNullException("exe");
            }

            if (dlls == null)
            {
                throw new ArgumentNullException("dlls");
            }

            if (string.IsNullOrWhiteSpace(configuration))
            {
                throw new ArgumentNullException("configuration");
            }

            if (string.IsNullOrWhiteSpace(platform))
            {
                throw new ArgumentNullException("platform");
            }

            FileInfo[] dllArray = dlls.ToArray();

            if (!dllArray.Any())
            {
                throw new ArgumentException("DLL list is empty", "dlls");
            }

            _exe = exe;
            _dlls = dllArray;
            _configuration = configuration;
            _platform = platform;
        }

        public string Configuration
        {
            get { return _configuration; }
        }

        public string Platform
        {
            get { return _platform; }
        }

        public string Exe
        {
            get { return _exe; }
        }

        public IEnumerable<FileInfo> Dlls
        {
            get { return _dlls; }
        }
    }
}