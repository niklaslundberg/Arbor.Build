using System;
using Arbor.FS;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.NuGet
{
    public static class NuSpecHelper
    {
        public static string IncludedFile([NotNull] FileEntry fileName,
            [NotNull] DirectoryEntry baseDirectory,
            ILogger logger)
        {
            if (baseDirectory is null)
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(baseDirectory));
            }

            if (!fileName.FullName.StartsWith(baseDirectory.FullName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The file must {fileName} be in base directory {baseDirectory}",
                    nameof(fileName));
            }

            fileName.EnsureHasValidDate(logger);

            int baseDirLength = baseDirectory.FullName.Length;

            string targetFilePath = fileName.FullName.Substring(baseDirLength).Replace('/', '\\');

            string fileNamePath = fileName.Path.WindowsPath();

            string fileItem = $@"<file src=""{fileNamePath}"" target=""Content\{targetFilePath}"" />";

            return fileItem.Replace("\\\\", "\\", StringComparison.Ordinal);
        }
    }
}