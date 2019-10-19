using System;
using System.IO;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class NuSpecHelper
    {
        public static string IncludedFile([NotNull] string fileName, [NotNull] string baseDirectory, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(baseDirectory));
            }

            if (!fileName.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The file must {fileName} be in base directory {baseDirectory}",
                    nameof(fileName));
            }

            if (Path.IsPathRooted(fileName))
            {
                var fileInfo = new FileInfo(fileName);

                var dateTime = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                if (fileInfo.Exists && fileInfo.LastWriteTimeUtc < dateTime)
                {
                    try
                    {
                        fileInfo.LastWriteTimeUtc = dateTime;
                        logger.Debug("Reset {LastWriteTime} for file {File} in nuspec",
                            nameof(fileInfo.LastWriteTimeUtc),
                            fileInfo.FullName);
                    }
                    catch (Exception ex)
                    {
                        logger.Debug(ex, "Could not reset {LastWriteTime} {File} in nuspec",
                            nameof(fileInfo.LastWriteTimeUtc),
                            fileInfo.FullName);
                    }
                }
            }

            int baseDirLength = baseDirectory.Length;
            string targetFilePath = fileName.Substring(baseDirLength);

            string fileItem = $@"<file src=""{fileName}"" target=""Content\{targetFilePath}"" />";

            return fileItem.Replace("\\\\", "\\", StringComparison.Ordinal);
        }
    }
}
