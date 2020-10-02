using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arbor.Build.Core.IO
{
    public static class StreamExtensions
    {
        public static async Task WriteAllTextAsync(this Stream stream,
            ReadOnlyMemory<char> text,
            Encoding? encoding = default,
            CancellationToken cancellationToken = default)
        {
            await using var streamWriter = new StreamWriter(stream, encoding ?? Encoding.UTF8);

            await streamWriter.WriteAsync(text, cancellationToken);
        }

        public static Task WriteAllTextAsync(this Stream stream,
            string text,
            Encoding? encoding = default,
            CancellationToken cancellationToken = default) =>
            WriteAllTextAsync(stream, text.AsMemory(), encoding, cancellationToken);

        public static async Task<string> ReadAllTextAsync(this Stream stream,
            Encoding? encoding = default,
            bool leaveOpen = false,
            CancellationToken cancellationToken = default)
        {
            using var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8, leaveOpen);

            return await streamReader.ReadToEndAsync();
        }
        public static async IAsyncEnumerable<string> EnumerateLinesAsync(this Stream stream,
            Encoding? encoding = default,
            bool leaveOpen = false)
        {
            using var streamReader = new StreamReader(stream, encoding ?? Encoding.UTF8, leaveOpen);

            while (true)
            {
                string? line = await streamReader.ReadLineAsync();

                if (line is null)
                {
                    yield break;
                }

                yield return line;
            };
        }
        public static async Task<ImmutableArray<string>> ReadAllLinesAsync(this Stream stream,
            Encoding? encoding = default,
            bool leaveOpen = false)
        {
            var lines = new List<string>();

            await foreach (string item in EnumerateLinesAsync(stream, encoding, leaveOpen))
            {
                lines.Add(item);
            }

            return lines.ToImmutableArray();

        }
    }
}