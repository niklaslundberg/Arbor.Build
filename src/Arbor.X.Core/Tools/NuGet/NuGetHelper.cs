using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.NuGet
{
    public class NuGetHelper
    {
        readonly ILogger _logger;

        public NuGetHelper(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> EnsureNuGetExeExistsAsync()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var targetFile = Path.Combine(baseDir, "nuget.exe");

            const int maxRetries = 3;
            
            if (!File.Exists(targetFile))
            {
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        await DownloadNuGetExeAsync(baseDir, targetFile);

                        return targetFile;
                    }
                    catch (Exception ex)
                    {   
                        _logger.WriteError(string.Format("Attempt {1}. Could not download nuget.exe. {0}", ex, i + 1));
                    }

                    const int waitTimeInSeconds = 2;
                    
                    _logger.Write(string.Format("Waiting {0} seconds to try again", waitTimeInSeconds));

                    await Task.Delay(TimeSpan.FromSeconds(waitTimeInSeconds));
                }
            }

            return targetFile;
        }

        async Task DownloadNuGetExeAsync(string baseDir, string targetFile)
        {
            var tempFile = Path.Combine(baseDir, string.Format("nuget.exe.{0}.tmp", Guid.NewGuid()));

            const string nugetExeUri = "https://nuget.org/nuget.exe";

            _logger.Write(string.Format("Downloading {0} to {1}", nugetExeUri, tempFile));
            try
            {
                using (var client = new HttpClient())
                {
                    using (var stream = await client.GetStreamAsync(nugetExeUri))
                    {
                        using (var fs = new FileStream(tempFile, FileMode.Create))
                        {
                            await stream.CopyToAsync(fs);
                        }
                    }

                }
            }
            finally
            {
                if (File.Exists(tempFile) && new FileInfo(tempFile).Length > 0)
                {
                    File.Copy(tempFile, targetFile, overwrite: true);
                    _logger.Write(string.Format("Copied {0} to {1}", tempFile, targetFile));
                    File.Delete(tempFile);
                    _logger.Write(string.Format("Deleted temp file {0}", tempFile));
                }
            }
        }
    }
}