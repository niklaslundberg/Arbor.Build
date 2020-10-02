using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zio;

namespace Arbor.Build.Core.IO
{
    public static class FileExtensions
    {
        public static async Task WriteAllTextAsync(this FileEntry fileEntry, string text, Encoding? encoding = default, CancellationToken cancellationToken = default)
        {
            fileEntry.Directory.EnsureExists();

            await using (Stream stream = fileEntry.Open(FileMode.Create, FileAccess.Write))
            {
                await stream.WriteAllTextAsync(text.AsMemory(), encoding, cancellationToken);
            }
        }
    }
}