using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.Logging;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NuGetHelper
    {
        readonly ILogger _logger;

        public NuGetHelper(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> EnsureNuGetExeExistsAsync(CancellationToken cancellationToken)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var targetFile = Path.Combine(baseDir, "nuget.exe");

            const int maxRetries = 4;

            var currentExePath = new FileInfo(targetFile);

            if (!File.Exists(targetFile))
            {
                var parentExePath = Path.Combine(currentExePath.Directory.Parent.FullName, currentExePath.Name);
                if (Alphaleonis.Win32.Filesystem.File.Exists(parentExePath))
                {
                    _logger.Write($"Found NuGet in path '{parentExePath}', skipping download");
                    return parentExePath;
                }

                _logger.Write($"'{targetFile}' does not exist, will try to download from nuget.org");

                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        string nugetExeUri = i < 2 ? "https://nuget.org/nuget.exe" : "https://www.nuget.org/nuget.exe";

                        await DownloadNuGetExeAsync(baseDir, targetFile, nugetExeUri, cancellationToken);

                        return targetFile;
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteError(string.Format("Attempt {1}. Could not download nuget.exe. {0}", ex, i + 1));
                    }

                    const int waitTimeInSeconds = 2;

                    _logger.Write(string.Format("Waiting {0} seconds to try again", waitTimeInSeconds));

                    await Task.Delay(TimeSpan.FromSeconds(waitTimeInSeconds), cancellationToken);
                }
            }

            return targetFile;
        }

        async Task DownloadNuGetExeAsync(string baseDir, string targetFile, string nugetExeUri, CancellationToken cancellationToken)
        {
            var tempFile = Path.Combine(baseDir, string.Format("nuget.exe.{0}.tmp", Guid.NewGuid()));

            _logger.WriteVerbose(string.Format("Downloading {0} to {1}", nugetExeUri, tempFile));
            try
            {
                using (var client = new HttpClient())
                {
                    using (var stream = await client.GetStreamAsync(nugetExeUri))
                    {
                        using (var fs = new FileStream(tempFile, FileMode.Create))
                        {
                            await stream.CopyToAsync(fs, 4096, cancellationToken);
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(tempFile) && new FileInfo(tempFile).Length > 0)
                {
                    File.Copy(tempFile, targetFile, overwrite: true);
                    _logger.WriteVerbose(string.Format("Copied {0} to {1}", tempFile, targetFile));
                    File.Delete(tempFile);
                    _logger.WriteVerbose(string.Format("Deleted temp file {0}", tempFile));
                }
            }
        }
    }
}