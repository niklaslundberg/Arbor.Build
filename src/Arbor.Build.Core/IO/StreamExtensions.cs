using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Build.Core.IO
{
    public static class StreamExtensions
    {
        public static async Task WriteAllTextAsync(this Stream stream, ReadOnlyMemory<char> text, Encoding? encoding = default, CancellationToken cancellationToken = default)
        {
            await using var streamWriter = new StreamWriter(stream, encoding ?? Encoding.UTF8);

            await streamWriter.WriteAsync(text, cancellationToken);
        }
    }
}