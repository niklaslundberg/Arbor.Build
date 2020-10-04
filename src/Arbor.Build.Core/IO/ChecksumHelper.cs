using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Zio;

namespace Arbor.Build.Core.IO
{
    public static class ChecksumHelper
    {
        public static async Task<FileListWithChecksumFile> CreateFileListForDirectory(DirectoryEntry baseDirectory)
        {
            var files = baseDirectory
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .OrderBy(file => file.FullName)
                .Select(file => new
                {
                    file = file.FullName[baseDirectory.FullName.Length..],
                    sha512Base64Encoded = GetFileHashSha512Base64Encoded(file)
                })
                .ToArray();

            string json = JsonConvert.SerializeObject(new {files}, Formatting.Indented);

            DirectoryEntry tempDirectory = new DirectoryEntry(baseDirectory.FileSystem, UPath.Combine(Path.GetTempPath().AsFullPath(), Guid.NewGuid().ToString()))
                .EnsureExists();

            var contentFilesFile = UPath.Combine(tempDirectory.Path, "contentFiles.json");

            await using var contentStream = tempDirectory.FileSystem.OpenFile(contentFilesFile, FileMode.Create, FileAccess.Write);

            await contentStream.WriteAllTextAsync(json);

            var contentFileEntry = new FileEntry(tempDirectory.FileSystem, contentFilesFile);
            string contentFilesFileChecksum = GetFileHashSha512Base64Encoded(contentFileEntry);

            var hashFilePath = UPath.Combine(tempDirectory.Path, "contentFiles.json.sha512");

            var hashFile = new FileEntry(tempDirectory.FileSystem, hashFilePath);

            var hashFs = hashFile.Open(FileMode.Create, FileAccess.Write);

            await hashFs.WriteAllTextAsync(contentFilesFileChecksum);

            return new FileListWithChecksumFile(contentFileEntry, hashFile);
        }

        private static string GetFileHashSha512Base64Encoded(FileEntry fileName)
        {
            using var hashAlgorithm = SHA512.Create();

            using var fs = fileName.Open(FileMode.Open, FileAccess.Read);
            byte[] fileHash = hashAlgorithm.ComputeHash(fs);

            return Convert.ToBase64String(fileHash);
        }
    }
}