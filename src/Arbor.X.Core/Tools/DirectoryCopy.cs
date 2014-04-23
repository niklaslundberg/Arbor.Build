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

            foreach (var file in sourceDirectory.GetFiles())
                file.CopyTo(Path.Combine(targetDir, file.Name), overwrite: true);

            foreach (var directory in sourceDirectory.GetDirectories())
                Copy(directory.FullName, Path.Combine(targetDir, directory.Name));
        }
    }
}