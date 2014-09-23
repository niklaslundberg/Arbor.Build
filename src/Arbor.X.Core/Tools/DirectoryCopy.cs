using System;
using System.IO;
using System.Threading.Tasks;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools
{
    public static class DirectoryCopy
    {
        public static async Task<ExitCode> CopyAsync(string sourceDir, string targetDir, ILogger optionalLogger = null)
        {
            ILogger logger = optionalLogger ?? new NullLogger();

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

                logger.WriteVerbose(string.Format("Copying file '{0}' to destination '{1}'", file.FullName, destFileName));

                try
                {
                    file.CopyTo(destFileName, overwrite: true);
                }
                catch (PathTooLongException ex)
                {
                    logger.WriteError(
                        string.Format("Could not copy file to '{0}', path length is too long ({1})", destFileName,
                            destFileName.Length) + " " + ex);
                }
                catch (Exception ex)
                {
                    logger.WriteError(
                        string.Format("Could not copy file '{0}' to destination '{1}'", file.FullName, destFileName) +
                        " " + ex);
                }
            }

            foreach (DirectoryInfo directory in sourceDirectory.GetDirectories())
            {
                await CopyAsync(directory.FullName, Path.Combine(targetDir, directory.Name));
            }

            return ExitCode.Success;
        }
    }
}