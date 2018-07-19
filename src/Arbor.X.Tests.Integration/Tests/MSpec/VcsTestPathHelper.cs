using System.IO;
using Arbor.Aesculus.Core;
using NCrunch.Framework;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    static class VcsTestPathHelper
    {
        public static string FindVcsRootPath()
        {
            if (NCrunchEnvironment.NCrunchIsResident())
            {
                return VcsPathHelper.FindVcsRootPath(new FileInfo(NCrunchEnvironment.GetOriginalSolutionPath())
                    .Directory?.FullName);
            }

            return VcsPathHelper.FindVcsRootPath();
        }
    }
}
