using System;
using System.IO;

namespace Arbor.X.Core.IO
{
    public static class DirectoryExtensions
    {
        public static DirectoryInfo EnsureExists(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
            {
                throw new ArgumentNullException("directoryInfo");
            }

            directoryInfo.Refresh();

            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            return directoryInfo;
        }

        public static void DeleteIfExists(this DirectoryInfo directoryInfo, bool recursive = true)
        {
            if (directoryInfo != null)
            {
                directoryInfo.Refresh();

                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(recursive);
                }

            }
        }
    }
}