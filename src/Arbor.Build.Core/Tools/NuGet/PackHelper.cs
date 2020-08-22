using System;
using System.IO;
using JetBrains.Annotations;
using Serilog;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class PackHelper
    {
        public static void EnsureHasValidDate([NotNull] this string fileName, ILogger? logger = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            if (Path.IsPathRooted(fileName) && File.Exists(fileName))
            {
                var fileInfo = new FileInfo(fileName);

                var dateTime = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                if (fileInfo.Exists && fileInfo.LastWriteTimeUtc < dateTime)
                {
                    try
                    {
                        fileInfo.LastWriteTimeUtc = dateTime;
                        logger?.Debug("Reset {LastWriteTime} for file {File} in nuspec",
                            nameof(fileInfo.LastWriteTimeUtc),
                            fileInfo.FullName);
                    }
                    catch (Exception ex)
                    {
                        logger?.Debug(ex, "Could not reset {LastWriteTime} {File} in nuspec",
                            nameof(fileInfo.LastWriteTimeUtc),
                            fileInfo.FullName);
                    }
                }
            }
        }
    }
}