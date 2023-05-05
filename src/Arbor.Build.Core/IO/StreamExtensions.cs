using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Arbor.Build.Core.IO;

public static class StreamExtensions
{
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
        }
    }
}