using System;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class NuSpecHelper
    {
        public static string IncludedFile([NotNull] string fileName, [NotNull] string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(baseDirectory));
            }

            if (!fileName.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The file must {fileName} be in base directory {baseDirectory}",
                    nameof(fileName));
            }

            int baseDirLength = baseDirectory.Length;
            string targetFilePath = fileName.Substring(baseDirLength);

            string fileItem = $@"<file src=""{fileName}"" target=""Content\{targetFilePath}"" />";

            return fileItem.Replace("\\\\", "\\", StringComparison.Ordinal);
        }
    }
}
