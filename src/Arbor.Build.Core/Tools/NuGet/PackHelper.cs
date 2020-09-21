using System;
using System.IO;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class PackHelper
    {
        public static void EnsureHasValidDate([NotNull] this FileEntry fileName, ILogger? logger = default)
        {
            if (!fileName.Path.IsRelative && fileName.Exists)
            {
                var dateTime = new DateTime(2000, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                if (fileName.Exists && fileName.LastWriteTime < dateTime)
                {
                    try
                    {
                        fileName.LastWriteTime = dateTime;
                        logger?.Debug("Reset {LastWriteTime} for file {File} in nuspec",
                            nameof(fileName.LastWriteTime),
                            fileName.FullName);
                    }
                    catch (Exception ex)
                    {
                        logger?.Debug(ex, "Could not reset {LastWriteTime} {File} in nuspec",
                            nameof(fileName.LastWriteTime),
                            fileName.FullName);
                    }
                }
            }
        }
    }
}