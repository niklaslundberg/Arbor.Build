using System;
using Arbor.Aesculus.Core;
using Arbor.Build.Core.IO;
using NCrunch.Framework;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.Tests.MSpec
{
    static class VcsTestPathHelper
    {
        public static DirectoryEntry FindVcsRootPath()
        {
            var startDir = NCrunchEnvironment.NCrunchIsResident()
                ? NCrunchEnvironment.GetOriginalSolutionPath().AsFullPath().GetDirectory()
                : UPath.Empty;

            return FindVcsRootPath(startDir);
        }

        public static DirectoryEntry FindVcsRootPath(UPath baseDir)
        {
            var fs = new PhysicalFileSystem();

            if (baseDir != UPath.Empty && !fs.DirectoryExists(baseDir))
            {
                throw new InvalidOperationException($"The base directory {baseDir} does not exist");
            }

            string? startDirectory = baseDir == UPath.Empty
                ? null
                : fs.ConvertPathToInternal(baseDir);

            string vcsRootPath = VcsPathHelper.FindVcsRootPath(startDirectory);


            if (string.IsNullOrWhiteSpace(vcsRootPath))
            {
                throw new InvalidOperationException("Could not find source root");
            }

            return new DirectoryEntry(fs, vcsRootPath.AsFullPath());
        }
    }
}