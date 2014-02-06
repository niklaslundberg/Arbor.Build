using System.Collections.Generic;
using System.IO;

namespace Arbor.X.Core.Tools.ILMerge
{
    public class ILMergeData
    {
        public string Configuration
        {
            get { return _configuration; }
        }

        public string Platform
        {
            get { return _platform; }
        }

        readonly IEnumerable<FileInfo> _dlls;
        readonly string _configuration;
        readonly string _platform;
        readonly string _exe;

        public ILMergeData(string exe, IEnumerable<FileInfo> dlls, string configuration, string platform)
        {
            _exe = exe;
            _dlls = dlls;
            _configuration = configuration;
            _platform = platform;
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