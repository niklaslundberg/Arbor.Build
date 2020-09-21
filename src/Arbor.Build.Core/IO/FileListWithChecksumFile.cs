using System;
using JetBrains.Annotations;

namespace Arbor.Build.Core.IO
{
    public class FileListWithChecksumFile
    {
        public FileListWithChecksumFile([NotNull] string contentFilesFile, string checksumFile)
        {
            if (string.IsNullOrWhiteSpace(contentFilesFile))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(contentFilesFile));
            }

            ContentFilesFile = contentFilesFile;
            ChecksumFile = checksumFile;
        }

        public string ContentFilesFile { get; }

        public string ChecksumFile { get; }
    }
}