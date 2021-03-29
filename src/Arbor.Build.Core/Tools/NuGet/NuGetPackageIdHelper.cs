using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.Build.Core.Tools.Git;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class NuGetPackageIdHelper
    {

        public static string CreateNugetPackageId(
            string basePackageId,
            string? packageNameSuffix = null)
        {
            if (string.IsNullOrWhiteSpace(basePackageId))
            {
                throw new ArgumentNullException(nameof(basePackageId));
            }

            return basePackageId + packageNameSuffix;
        }
    }
}
