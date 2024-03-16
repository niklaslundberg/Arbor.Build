using Zio;

namespace Arbor.Build.Core.IO;

public class FileListWithChecksumFile(FileEntry contentFilesFile, FileEntry checksumFile)
{
    public FileEntry ContentFilesFile { get; } = contentFilesFile;

    public FileEntry ChecksumFile { get; } = checksumFile;
}