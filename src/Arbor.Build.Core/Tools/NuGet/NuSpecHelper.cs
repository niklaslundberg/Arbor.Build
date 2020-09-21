using System;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class NuSpecHelper
    {
        public static string IncludedFile([NotNull] FileEntry fileName, [NotNull] string baseDirectory, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(baseDirectory));
            }

            if (!fileName.FullName.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The file must {fileName} be in base directory {baseDirectory}",
                    nameof(fileName));
            }

            fileName.EnsureHasValidDate(logger);

            int baseDirLength = baseDirectory.Length;
            string targetFilePath = fileName.FullName.Substring(baseDirLength);

            string fileItem = $@"<file src=""{fileName}"" target=""Content\{targetFilePath}"" />";

            return fileItem.Replace("\\\\", "\\", StringComparison.Ordinal);
        }
    }
}
