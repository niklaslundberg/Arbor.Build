using System;
using System.IO;
using Arbor.X.Core.IO;

namespace Arbor.X.Core.Tools
{
    public static class DirectoryCopy
    {
        public static void Copy(string sourceDir, string targetDir)
        {
            if (string.IsNullOrWhiteSpace(sourceDir))
            {
                throw new ArgumentNullException("sourceDir");
            }

            if (string.IsNullOrWhiteSpace(targetDir))
            {
                throw new ArgumentNullException("targetDir");
            }

            var sourceDirectory = new DirectoryInfo(sourceDir);

            if (!sourceDirectory.Exists)
            {
                throw new ArgumentException(string.Format("Source directory '{0}' does not exist", sourceDir));
            }

            new DirectoryInfo(targetDir).EnsureExists();

            foreach (FileInfo file in sourceDirectory.GetFiles())
            {
                string destFileName = Path.Combine(targetDir, file.Name);

                try
                {
                    file.CopyTo(destFileName, overwrite: true);
                }
                catch (PathTooLongException ex)
                {
                    throw new PathTooLongException(
                        string.Format("Could not copy file to '{0}', path length {1}", destFileName, destFileName.Length),
                        ex);
                }
            }

            foreach (DirectoryInfo directory in sourceDirectory.GetDirectories())
            {
                Copy(directory.FullName, Path.Combine(targetDir, directory.Name));
            }
        }
    }
}