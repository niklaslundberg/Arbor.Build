using System;
using System.IO;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Libz
{
    public sealed class CurrentDirectoryScope : IDisposable
    {
        private readonly DirectoryInfo _originalDirectory;

        private DirectoryInfo _newDirectoryInfo;

        private CurrentDirectoryScope(DirectoryInfo originalDirectory, DirectoryInfo newDirectoryInfo)
        {
            _originalDirectory = originalDirectory;
            _newDirectoryInfo = newDirectoryInfo;
        }

        public static CurrentDirectoryScope Create(
            [NotNull] DirectoryInfo originalDirectory,
            [NotNull] DirectoryInfo newDirectoryInfo)
        {
            if (originalDirectory == null)
            {
                throw new ArgumentNullException(nameof(originalDirectory));
            }

            if (newDirectoryInfo == null)
            {
                throw new ArgumentNullException(nameof(newDirectoryInfo));
            }

            if (!originalDirectory.Exists)
            {
                throw new InvalidOperationException(
                    $"The original directory '{originalDirectory.FullName}' does not exist");
            }

            if (!newDirectoryInfo.Exists)
            {
                throw new InvalidOperationException($"The new directory '{newDirectoryInfo.FullName}' does not exist");
            }

            Directory.SetCurrentDirectory(newDirectoryInfo.FullName);

            return new CurrentDirectoryScope(originalDirectory, newDirectoryInfo);
        }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(_originalDirectory.FullName);
        }
    }
}
