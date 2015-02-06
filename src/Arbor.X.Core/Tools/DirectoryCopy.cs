using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arbor.X.Core.IO;
using ILogger = Arbor.X.Core.Logging.ILogger;
using NullLogger = Arbor.X.Core.Logging.NullLogger;

namespace Arbor.X.Core.Tools
{
    public static class DirectoryCopy
    {
        public static async Task<ExitCode> CopyAsync(string sourceDir, string targetDir, ILogger optionalLogger = null, PathLookupSpecification pathLookupSpecificationOption = null)
        {
            var pathLookupSpecification = pathLookupSpecificationOption ?? DefaultPaths.DefaultPathLookupSpecification;

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

            if (pathLookupSpecification.IsBlackListed(sourceDir))
            {
                logger.WriteDebug(string.Format("Directory '{0}' is blacklisted from specification {1}", sourceDir, pathLookupSpecification.ToString()));
                return ExitCode.Success;
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
                    return ExitCode.Failure;
                }
                catch (Exception ex)
                {
                    logger.WriteError(
                        string.Format("Could not copy file '{0}' to destination '{1}'", file.FullName, destFileName) +
                        " " + ex);
                    return ExitCode.Failure;
                }
            }

            foreach (DirectoryInfo directory in sourceDirectory.GetDirectories())
            {
                var exitCode = await CopyAsync(directory.FullName, Path.Combine(targetDir, directory.Name), pathLookupSpecificationOption: pathLookupSpecification);

                if (!exitCode.IsSuccess)
                {
                    return exitCode;
                }
            }

            return ExitCode.Success;
        }

    }
}