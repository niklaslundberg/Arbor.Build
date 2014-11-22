using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Sorbus.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.ProcessUtils;
using Semver;
using ILogger = Arbor.X.Core.Logging.ILogger;

namespace Arbor.X.Core.Tools.Sass
{
    [Priority(299)]
    public class SassCompiler : ITool
    {
        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            //if (!buildVariables.GetBooleanByKey(WellKnownVariables.NodeJsExePath, false))
            //{
            //    return ExitCode.Success;
            //}

            FileInfo[] scssFiles =
                new DirectoryInfo(buildVariables.GetVariable(WellKnownVariables.SourceRoot).Value).GetFiles("*.scss",
                    SearchOption.AllDirectories);

            if (!scssFiles.Any())
            {
                return ExitCode.Success;
            }

            string nodeExePath = buildVariables.GetVariableValueOrDefault(WellKnownVariables.NodeJsExePath, "");

            if (!string.IsNullOrWhiteSpace(nodeExePath))
            {
                logger.WriteVerbose("Found node exe path " + nodeExePath);
                return ExitCode.Success;
            }

            IEnumerable<string> arguments = new List<string>() {"-v"};

            bool hasNode = false;

            try
            {
                ExitCode exitCode =
                    await
                        ProcessRunner.ExecuteAsync(executePath: nodeExePath, arguments: arguments,
                            cancellationToken: cancellationToken, logger: logger);

                hasNode = exitCode.IsSuccess;
            }
            catch (Exception ex)
            {
                logger.WriteDebug(ex.ToString());
            }
            
            if (!hasNode)
            {
                try
                {
                   nodeExePath = await DownloadNodeJsAsync(cancellationToken);

                    if (string.IsNullOrWhiteSpace(nodeExePath))
                    {
                        return ExitCode.Failure;
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteError(ex.ToString());
                    return ExitCode.Failure;
                }
            }
            try
            {
                await GetNpmAsync(nodeExePath,cancellationToken);
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.ToString());
                return ExitCode.Failure;
            }

            var nodeDir = new FileInfo(nodeExePath).Directory.FullName;

            var exeFile = Path.Combine(nodeDir, "npm.cmd");
            var arguments2 = new List<string> {"npm install node-sass"};
            try
            {
                var exitCode=  await ProcessRunner.ExecuteAsync(exeFile, arguments:arguments2, logger:logger, cancellationToken: cancellationToken);

                if (!exitCode.IsSuccess)
                {
                    return exitCode;
                }
            }
            catch (Exception ex)
            {
                logger.WriteError(ex.ToString());
                return ExitCode.Failure;
            }


            return ExitCode.Success;
        }

        static async Task GetNpmAsync(string nodeExePath, CancellationToken cancellationToken)
        {
            const string npmDownloadPage = "https://nodejs.org/dist/npm/";


            using (var httpClient = new HttpClient())
            {
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(npmDownloadPage, cancellationToken);


                string content = await httpResponseMessage.Content.ReadAsStringAsync();

                string[] lines = content.Split(new[] {Environment.NewLine, "\n"}, StringSplitOptions.RemoveEmptyEntries);

                var downloadLines = lines.Where(
                    line => line.IndexOf("href", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    .Select(line =>
                    {
                        string href = "href=\"";
                        string linkStart =
                            line.Substring(href.Length + line.IndexOf(href, StringComparison.InvariantCultureIgnoreCase));

                        string link = linkStart.Substring(0, linkStart.IndexOf('"'));

                        return link;
                    })
                    .Where(link => link.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                    .Select(link =>
                    {
                        string version = link.Replace("npm-", "").Replace(".zip", "");

                        return new {Link = npmDownloadPage + link, Version = version};
                    }).ToReadOnly();

                var withSemVer =
                    downloadLines.Select(item => new {Link = item.Link, SemVer = SemVersion.Parse(item.Version)}).ToReadOnly();

                SemVersion maxVersion = withSemVer.Max(item => item.SemVer);

                string uri = withSemVer.Single(item => item.SemVer == maxVersion).Link;
                var nodeDirectory = new FileInfo(nodeExePath).Directory;

                var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Arbor.X", "NodeJSTemp")).EnsureExists();
                var npmFile = Path.Combine(tempDir.FullName, "npm.zip");

                using (Stream stream = await httpClient.GetStreamAsync(uri))
                {
                    using (var fileStream = new FileStream(npmFile, FileMode.Create, FileAccess.Write))
                    {
                        await stream.CopyToAsync(fileStream);
                        await fileStream.FlushAsync(cancellationToken);
                    }
                }

                ZipFile.ExtractToDirectory(npmFile, nodeDirectory.FullName);
            }
        }

        static async Task<string> DownloadNodeJsAsync(CancellationToken cancellationToken)
        {
            using (var httpClient = new HttpClient())
            {
                HttpResponseMessage response =
                    await httpClient.GetAsync("https://nodejs.org/download/", cancellationToken);

                string content = await response.Content.ReadAsStringAsync();

                string[] lines = content.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries);

                IReadOnlyCollection<string> x64ExeLine =
                    ReadOnlyCollectionExtension.ToReadOnly(lines.Where(line =>
                        line.IndexOf("x64/node.exe", StringComparison.InvariantCultureIgnoreCase) >= 0));

                if (x64ExeLine.Any())
                {
                    string line = x64ExeLine.First().Trim();

                    string http = line.Substring(line.IndexOf("http", StringComparison.InvariantCultureIgnoreCase));

                    string uri = http.Substring(0, http.IndexOf('"'));

                    UriBuilder uriBuilder = new UriBuilder(uri) {Scheme = "https", Port = 443};

                    string nodeTempPath = Path.Combine(Path.GetTempPath(), "Arbor.X", "NodeJS",
                        Guid.NewGuid().ToString());

                    DirectoryInfo nodeTempDirectory = new DirectoryInfo(nodeTempPath).EnsureExists();

                    string nodeExeFile = Path.Combine(nodeTempDirectory.FullName, "node.exe");

                    using (Stream stream = await httpClient.GetStreamAsync(uriBuilder.Uri))
                    {
                        using (var fileStream = new FileStream(nodeExeFile, FileMode.Create, FileAccess.Write))
                        {
                            await stream.CopyToAsync(fileStream);
                            await fileStream.FlushAsync(cancellationToken);
                        }
                    }

                    return nodeExeFile;
                }
            }
            return null;
        }
    }
}