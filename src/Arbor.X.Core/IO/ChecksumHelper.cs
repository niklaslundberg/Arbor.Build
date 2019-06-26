using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Arbor.Build.Core.IO
{
    public static class ChecksumHelper
    {
        public static FileListWithChecksumFile CreateFileListForDirectory([NotNull] DirectoryInfo baseDirectory)
        {
            if (baseDirectory == null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            var files = baseDirectory
                .GetFiles("*", SearchOption.AllDirectories)
                .OrderBy(file => file.FullName)
                .Select(file => file.FullName)
                .Select(file => new
                {
                    file = file.Substring(baseDirectory.FullName.Length),
                    sha512Base64Encoded = GetFileHashSha512Base64Encoded(file)
                })
                .ToArray();

            string json = JsonConvert.SerializeObject(new { files }, Formatting.Indented);

            DirectoryInfo tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                .EnsureExists();

            string contentFilesFile = Path.Combine(tempDirectory.FullName, "contentFiles.json");

            File.WriteAllText(contentFilesFile, json, Encoding.UTF8);

            string contentFilesFileChecksum = GetFileHashSha512Base64Encoded(contentFilesFile);

            string hashFile = Path.Combine(tempDirectory.FullName, "contentFiles.json.sha512");

            File.WriteAllText(hashFile, contentFilesFileChecksum, Encoding.UTF8);

            return new FileListWithChecksumFile(contentFilesFile, hashFile);
        }

        private static string GetFileHashSha512Base64Encoded(string fileName)
        {
            byte[] fileHash;
            using (SHA512 hashAlgorithm = SHA512.Create())
            {
                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    fileHash = hashAlgorithm.ComputeHash(fs);
                }
            }

            return Convert.ToBase64String(fileHash);
        }
    }

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
