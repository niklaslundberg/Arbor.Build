using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.VisualStudio
{
    [Priority(53)]
    [UsedImplicitly]
    public class VisualStudioEnvironmentVerification : ITool
    {
        private readonly BuildContext _buildContext;

        public VisualStudioEnvironmentVerification(BuildContext buildContext) => _buildContext = buildContext;

        public Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            string[] args,
            CancellationToken cancellationToken)
        {
            var rootDir = _buildContext.SourceRoot;

            string visualStudioVersion =
                buildVariables.Require(WellKnownVariables.ExternalTools_VisualStudio_Version).GetValueOrThrow();

            if (!visualStudioVersion.Equals("12.0", StringComparison.Ordinal))
            {
                string[] extensionPatterns = { ".csproj", ".vcxproj" };

                IEnumerable<FileEntry> projectFiles = rootDir.EnumerateFiles()
                    .Where(
                        file =>
                            extensionPatterns.Any(
                                pattern => file.ExtensionWithDot.Equals(
                                    pattern,
                                    StringComparison.OrdinalIgnoreCase)));

                List<FileEntry> projectFiles81 = projectFiles.Where(Contains81).ToList();

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

        private bool Contains81(FileEntry file)
        {
            var lookupPatterns = new[]
            {
                "<ApplicationTypeRevision>8.1</ApplicationTypeRevision>",
                "<TargetPlatformVersion>8.1</TargetPlatformVersion>"
            };

            using var fs = file.Open(FileMode.Open, FileAccess.Read);

            using var sr = new StreamReader(fs);
            while (sr.Peek() >= 0)
            {
                string? line = sr.ReadLine();

                if (!string.IsNullOrWhiteSpace(line) && lookupPatterns.Any(
                    pattern => line.Contains(pattern, StringComparison.InvariantCulture)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
