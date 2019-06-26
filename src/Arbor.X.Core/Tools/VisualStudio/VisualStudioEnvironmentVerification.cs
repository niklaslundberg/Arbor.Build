using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;

namespace Arbor.Build.Core.Tools.VisualStudio
{
    [Priority(53)]
    [UsedImplicitly]
    public class VisualStudioEnvironmentVerification : ITool
    {
        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            logger = logger ?? Logger.None;

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            string visualStudioVersion =
                buildVariables.Require(WellKnownVariables.ExternalTools_VisualStudio_Version).ThrowIfEmptyValue().Value;

            if (!visualStudioVersion.Equals("12.0", StringComparison.Ordinal))
            {
                var rootDir = new DirectoryInfo(sourceRoot);

                string[] extensionPatterns = { ".csproj", ".vcxproj" };

                IEnumerable<FileInfo> projectFiles = rootDir.EnumerateFiles()
                    .Where(
                        file =>
                            extensionPatterns.Any(
                                pattern => file.Extension.Equals(
                                    pattern,
                                    StringComparison.OrdinalIgnoreCase)));

                List<FileInfo> projectFiles81 = projectFiles.Where(Contains81).ToList();

                if (projectFiles81.Count > 0)
                {
                    IEnumerable<string> projectFileNames = projectFiles81.Select(file => file.FullName);

                    logger.Error(
                        "Visual Studio version {VisualStudioVersion} is found on this machine. Visual Studio 12.0 (2013) must be installed in order to build these projects: {NewLine}{V}",
                        visualStudioVersion,
                        Environment.NewLine,
                        string.Join(Environment.NewLine, projectFileNames));
                    return Task.FromResult(ExitCode.Failure);
                }
            }

            return Task.FromResult(ExitCode.Success);
        }

        private bool Contains81(FileInfo file)
        {
            var lookupPatterns = new[]
            {
                "<ApplicationTypeRevision>8.1</ApplicationTypeRevision>",
                "<TargetPlatformVersion>8.1</TargetPlatformVersion>"
            };

            using (FileStream fs = file.OpenRead())
            {
                using (var sr = new StreamReader(fs))
                {
                    while (sr.Peek() >= 0)
                    {
                        string line = sr.ReadLine();

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            if (
                                lookupPatterns.Any(
                                    pattern => line.IndexOf(pattern, StringComparison.InvariantCulture) >= 0))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
