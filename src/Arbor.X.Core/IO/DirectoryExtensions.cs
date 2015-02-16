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
            try
            {
                if (directoryInfo != null)
                {
                    directoryInfo.Refresh();

                    if (directoryInfo.Exists)
                    {
                        foreach (var file in directoryInfo.EnumerateFiles())
                        {
                            file.Attributes = FileAttributes.Normal;
                            file.Delete();
                        }

                        foreach (var subDirectory in directoryInfo.EnumerateDirectories())
                        {
                            subDirectory.DeleteIfExists(recursive);
                        }
                    }

                    directoryInfo.Refresh();
                    if (directoryInfo.Exists)
                    {
                        directoryInfo.Delete(recursive);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(string.Format("Could not delete directory '{0}'", directoryInfo.FullName), ex);
            }
        }
    }
}