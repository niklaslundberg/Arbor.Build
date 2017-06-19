using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Parsing;
using Arbor.X.Core.Tools.ILRepack;
using FubuCsProjFile;
using FubuCsProjFile.MSBuild;
using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.Libz
{
    [Priority(620)]
    [UsedImplicitly]

    // ReSharper disable once InconsistentNaming
    public class LibZPacker : ITool
    {
        private string _artifactsPath;
        private string _exePath;
        private ILogger _logger;

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _logger = logger;

            ParseResult<bool> parseResult = buildVariables
                .GetVariableValueOrDefault(WellKnownVariables.ExternalTools_LibZ_Enabled, "false")
                .TryParseBool(false);

            if (!parseResult.Value)
            {
                _logger.Write(
                    $"LibZPacker is disabled, to enable it, set the flag {WellKnownVariables.ExternalTools_LibZ_Enabled} to true");
                return ExitCode.Success;
            }

            _exePath = buildVariables.Require(WellKnownVariables.ExternalTools_LibZ_ExePath)
                .ThrowIfEmptyValue()
                .Value;

            string customExePath = buildVariables.GetVariableValueOrDefault(
                WellKnownVariables.ExternalTools_LibZ_Custom_ExePath,
                string.Empty);

            if (!string.IsNullOrWhiteSpace(customExePath) && File.Exists(customExePath))
            {
                logger.Write($"Using custom path for LibZ: '{customExePath}'");
                _exePath = customExePath;
            }

            _artifactsPath =
                buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;

            string sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var sourceRootDirectory = new DirectoryInfo(sourceRoot);

            List<FileInfo> csharpProjectFiles =
                sourceRootDirectory.GetFilesRecursive(
                        new List<string> { ".csproj" },
                        DefaultPaths.DefaultPathLookupSpecification,
                        sourceRoot)
                    .ToList();

            List<FileInfo> ilMergeProjects = csharpProjectFiles.Where(IsMergeEnabledInProjectFile).ToList();

            string merges = string.Join(Environment.NewLine, ilMergeProjects.Select(item => item.FullName));

            logger.Write($"Found {ilMergeProjects.Count} projects marked for merging:{Environment.NewLine}{merges}");

            IReadOnlyCollection<ILRepackData> filesToMerge =
                ilMergeProjects.SelectMany(GetMergeFiles).ToReadOnlyCollection();

            foreach (ILRepackData repackData in filesToMerge)
            {
                var fileInfo = new FileInfo(repackData.Exe);

                string mergedDirectoryPath = Path.Combine(
                    _artifactsPath,
                    "LibZ",
                    repackData.Platform,
                    repackData.Configuration);

                var mergedDirectory = new DirectoryInfo(mergedDirectoryPath).EnsureExists();

                string mergedPath = Path.Combine(mergedDirectory.FullName, fileInfo.Name);

                fileInfo.CopyTo(mergedPath);

                var arguments = new List<string>
                {
                    "inject-dll",
                    "--assembly",
                    $"{mergedPath}"
                };

                foreach (FileInfo dll in repackData.Dlls)
                {
                    arguments.Add("--include");
                    arguments.Add(dll.FullName);
                }

                arguments.Add("--move");

                ExitCode result;

                using (CurrentDirectoryScope.Create(
                    new DirectoryInfo(Directory.GetCurrentDirectory()),
                    mergedDirectory))
                {
                    result = await ProcessRunner.ExecuteAsync(
                        _exePath,
                        arguments: arguments,
                        standardOutLog: logger.Write,
                        toolAction: logger.Write,
                        standardErrorAction: logger.WriteError,
                        cancellationToken: cancellationToken);
                }

                if (!result.IsSuccess)
                {
                    logger.WriteError($"Could not LibZ '{fileInfo.FullName}'");
                    return result;
                }

                logger.Write($"LibZ result: {mergedPath}");
            }

            return ExitCode.Success;
        }

        private static bool FileIsStandAloneExe(FileInfo file)
        {
            return file.Name.IndexOf(".vshost.", StringComparison.InvariantCultureIgnoreCase) < 0;
        }

        private IEnumerable<ILRepackData> GetMergeFiles(FileInfo projectFile)
        {
// ReSharper disable PossibleNullReferenceException
            DirectoryInfo binDirectory = projectFile.Directory.GetDirectories("bin").SingleOrDefault();

            // ReSharper restore PossibleNullReferenceException

            if (binDirectory == null)
            {
                yield break;
            }

            string configuration = "release"; // TODO support ilmerge for debug

            DirectoryInfo releaseDir = binDirectory.GetDirectories(configuration).SingleOrDefault();

            if (releaseDir is null)
            {
                _logger.WriteWarning($"The release directory '{Path.Combine(binDirectory.FullName, configuration)}' does not exist");
                yield break;
            }

            DirectoryInfo[] releasePlatformDirectories = releaseDir.GetDirectories();

            if (releasePlatformDirectories.Length > 1)
            {
                _logger.WriteWarning(
                    $"Multiple release directories were found for  '{Path.Combine(binDirectory.FullName, configuration)}'");
                yield break;
            }

            if (!releasePlatformDirectories.Any())
            {
               yield break;
            }

            DirectoryInfo releasePlatformDirectory = releasePlatformDirectories.Single();

            string NormalizeVersion(string value)
            {
                if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    return value.Substring(1);
                }

                return value;
            }

            string targetFrameworkVersionValue;

            if (File.ReadLines(projectFile.FullName)
                .Any(line => line.Contains("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.WriteWarning(
                    $"Microsoft.NET.Sdk projects are not supported '{Path.Combine(binDirectory.FullName, configuration)}' was not found");

                targetFrameworkVersionValue = "";
            }
            else
            {
                CsProjFile csProjFile = CsProjFile.LoadFrom(projectFile.FullName);

                const string targetFrameworkVersion = "TargetFrameworkVersion";

                MSBuildProperty msBuildProperty = csProjFile.BuildProject.PropertyGroups
                    .SelectMany(group =>
                        group.Properties.Where(
                            property => property.Name.Equals(targetFrameworkVersion,
                                StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(msBuildProperty?.Value))
                {
                    throw new InvalidOperationException(
                        $"The CSProj file '{csProjFile.FileName}' does not contain a property '{targetFrameworkVersion}");
                }

                targetFrameworkVersionValue = NormalizeVersion(msBuildProperty.Value);
            }

            List<FileInfo> exes = releasePlatformDirectory
                .EnumerateFiles("*.exe")
                .Where(FileIsStandAloneExe)
                .ToList();

            if (exes.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Only one exe can be merged, found {string.Join(", ", exes.Select(file => file.FullName))}");
            }

            if (!exes.Any())
            {
                throw new InvalidOperationException("Could not find any exe files to merge");
            }

            FileInfo exe = exes.Single();

            string platform = GetPlatform(exe);

            ImmutableArray<FileInfo> dlls = releasePlatformDirectory
                .EnumerateFiles("*.dll")
                .Where(FileIsStandAloneExe)
                .ToImmutableArray();

            yield return new ILRepackData(exe.FullName, dlls, configuration, platform, targetFrameworkVersionValue);
        }

        private string GetPlatform(FileInfo exe)
        {
            Assembly assembly = Assembly.LoadFile(Path.GetFullPath(exe.FullName));
            Module manifestModule = assembly.ManifestModule;
            PortableExecutableKinds peKind;
            ImageFileMachine machine;
            manifestModule.GetPEKind(out peKind, out machine);

            if (peKind == PortableExecutableKinds.ILOnly)
            {
                return "AnyCPU";
            }

            switch (machine)
            {
                case ImageFileMachine.I386:
                    return "x86";
            }

            throw new InvalidOperationException($"Could not find out the platform for the file '{exe.FullName}'");
        }

        private bool IsMergeEnabledInProjectFile(FileInfo file)
        {
            using (FileStream fs = file.OpenRead())
            {
                using (var streamReader = new StreamReader(fs))
                {
                    while (streamReader.Peek() >= 0)
                    {
                        string line = streamReader.ReadLine();

                        if (line?.IndexOf(
                                "<ILMergeExe>true</ILMergeExe>",
                                StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
