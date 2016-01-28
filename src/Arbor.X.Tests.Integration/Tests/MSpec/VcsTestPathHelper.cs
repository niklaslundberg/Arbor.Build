using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Arbor.Aesculus.Core;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    class VcsTestPathHelper
    {
        public static string FindVcsRootPath()
        {
            try
            {
                Assembly ncrunchAssembly = AppDomain.CurrentDomain.Load("NCrunch.Framework");

                Type ncrunchType =
                    ncrunchAssembly.GetTypes()
                        .FirstOrDefault(
                            type => type.Name.Equals("NCrunchEnvironment", StringComparison.InvariantCultureIgnoreCase));

                MethodInfo method = ncrunchType?.GetMethod("GetOriginalSolutionPath");

                string originalSolutionPath = method?.Invoke(null, null) as string;

                if (!string.IsNullOrWhiteSpace(originalSolutionPath))
                {
                    DirectoryInfo parent = new DirectoryInfo(originalSolutionPath).Parent;
                    if (parent != null)
                    {
                        return VcsPathHelper.FindVcsRootPath(parent.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine("Could not find NCrunch original solution path, {0}", ex);
#endif
            }
            return VcsPathHelper.FindVcsRootPath();
        }
    }
}
