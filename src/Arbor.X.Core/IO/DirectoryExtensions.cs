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

            try
            {
                directoryInfo.Refresh();

                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
            }
            catch (PathTooLongException ex)
            {
                throw new PathTooLongException(
                    string.Format("Could not create directory '{0}', path length {1}", directoryInfo.FullName,
                        directoryInfo.FullName.Length), ex);
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