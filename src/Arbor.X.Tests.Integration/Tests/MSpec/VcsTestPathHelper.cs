using System;
using System.IO;
using Arbor.Aesculus.Core;
using NCrunch.Framework;

namespace Arbor.Build.Tests.Integration.Tests.MSpec
{
    static class VcsTestPathHelper
    {
        public static string FindVcsRootPath(string baseDir = default)
        {
            if (NCrunchEnvironment.NCrunchIsResident())
            {
                return VcsPathHelper.FindVcsRootPath(new FileInfo(NCrunchEnvironment.GetOriginalSolutionPath())
                    .Directory?.FullName);
            }

            var startDir = baseDir ?? "N/A";
            Console.WriteLine($"Finding source root dir from start directory {startDir}");

            return VcsPathHelper.FindVcsRootPath(baseDir);
        }
    }
}
