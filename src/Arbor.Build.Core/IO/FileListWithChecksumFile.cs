using System;
using JetBrains.Annotations;
using Zio;

namespace Arbor.Build.Core.IO
{
    public class FileListWithChecksumFile
    {
        public FileListWithChecksumFile(FileEntry contentFilesFile, FileEntry checksumFile)
        {
            ContentFilesFile = contentFilesFile;
            ChecksumFile = checksumFile;
        }

        public FileEntry ContentFilesFile { get; }

        public FileEntry ChecksumFile { get; }
    }
}