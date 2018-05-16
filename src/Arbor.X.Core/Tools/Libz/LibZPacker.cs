using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Parsing;
using Arbor.X.Core.ProcessUtils;
using Arbor.X.Core.Tools.ILRepack;
using FubuCore;
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

            ImmutableArray<ILRepackData> filesToMerge;
            try
            {
                filesToMerge = (await Task.WhenAll(ilMergeProjects.Select(GetMergeFilesAsync)))
                    .SelectMany(item => item).ToImmutableArray();
            }
            catch (Exception ex)
            {
                _logger.WriteError(ex.ToString());

                return ExitCode.Failure;
            }

            foreach (ILRepackData repackData in filesToMerge)
            {
                var fileInfo = new FileInfo(repackData.Exe);

                string mergedDirectoryPath = Path.Combine(
                    _artifactsPath,
                    "LibZ",
                    repackData.Platform,
                    repackData.Configuration);

                DirectoryInfo mergedDirectory = new DirectoryInfo(mergedDirectoryPath).EnsureExists();

                string mergedPath = Path.Combine(mergedDirectory.FullName, fileInfo.Name);

                fileInfo.CopyTo(mergedPath);

                string exeConfiguration =
                    Path.Combine(fileInfo.Directory.FullName, $"{fileInfo.Name}.config");

                if (File.Exists(exeConfiguration))
                {
                    string targetConfigFilePath =
                        Path.Combine(mergedDirectory.FullName, Path.GetFileName(exeConfiguration));

                    File.Copy(exeConfiguration, targetConfigFilePath);
                }

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
            var blacklisted = new List<string> { ".vshost.", "csc.exe", "csi.exe", "vbc.exe", "VBCSCompiler.exe" };

            return !blacklisted.Any(
                blacklistedItem => file.Name.IndexOf(blacklistedItem, StringComparison.InvariantCultureIgnoreCase) >=
                                   0);
        }

        private static bool IsNetSdkProject(FileInfo projectFile)
        {
            return File.ReadLines(projectFile.FullName)
                .Any(line => line.Contains("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<ImmutableArray<ILRepackData>> GetMergeFilesAsync(FileInfo projectFile)
        {
// ReSharper disable PossibleNullReferenceException
            DirectoryInfo binDirectory = projectFile.Directory.GetDirectories("bin").SingleOrDefault();

            // ReSharper restore PossibleNullReferenceException

            if (binDirectory == null)
            {
                return ImmutableArray<ILRepackData>.Empty;
            }

            string configuration = "release"; // TODO support ilmerge for debug

            DirectoryInfo releaseDir = binDirectory.GetDirectories(configuration).SingleOrDefault();

            if (releaseDir is null)
            {
                _logger.WriteWarning(
                    $"The release directory '{Path.Combine(binDirectory.FullName, configuration)}' does not exist");
                return ImmutableArray<ILRepackData>.Empty;
            }

            DirectoryInfo[] releasePlatformDirectories = releaseDir.GetDirectories();

            if (releasePlatformDirectories.Length > 1)
            {
                _logger.WriteWarning(
                    $"Multiple release directories were found for  '{Path.Combine(binDirectory.FullName, configuration)}'");
                return ImmutableArray<ILRepackData>.Empty;
            }

            string NormalizeVersion(string value)
            {
                if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    return value.Substring(1);
                }

                return value;
            }

            string targetFrameworkVersionValue = null;

            DirectoryInfo releasePlatformDirectory = null;

            bool useSdkProject = IsNetSdkProject(projectFile);

            if (useSdkProject)
            {
                _logger.WriteWarning(
                    $"Microsoft.NET.Sdk projects are in progress supported '{Path.Combine(binDirectory.FullName, configuration)}'");

                targetFrameworkVersionValue = string.Empty;

                if (!releasePlatformDirectories.Any())
                {
                    _logger.WriteWarning(
                        $"No release platform directories were found in '{Path.Combine(binDirectory.FullName, configuration)}'");
                    return ImmutableArray<ILRepackData>.Empty;
                }

                if (releasePlatformDirectories.Length == 1 && releasePlatformDirectories[0].Name
                        .StartsWith("net4", StringComparison.OrdinalIgnoreCase))
                {
                    var netFrameworkMappings =
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "net452", "4.5.2" },
                            { "net461", "4.6.1" },
                            { "net462", "4.6.2" },
                            { "net471", "4.7.1" },
                            { "net47", "4.7" },
                            { "net472", "4.7.2" },
                            { "net48", "4.8" }
                        };

                    targetFrameworkVersionValue = netFrameworkMappings.ContainsKey(releasePlatformDirectories[0].Name) ? releasePlatformDirectories[0].Name : "4.0";

                    releasePlatformDirectory = releasePlatformDirectories[0];
                }
                else
                {
                    var args = new List<string>
                    {
                        "publish",
                        projectFile.FullName,
                        $"/p:configuration={configuration}"
                    };

                    ExitCode exitCode = await ProcessHelper.ExecuteAsync(
                        Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                            "dotnet",
                            "dotnet.exe"),
                        args,
                        _logger);

                    if (!exitCode.IsSuccess)
                    {
                        _logger.WriteWarning($"Could not publish project {projectFile.FullName}");
                        throw new InvalidOperationException("Failed to get merge files for LibZ");
                    }

                    DirectoryInfo platformDirectory = releasePlatformDirectories.Single();

                    DirectoryInfo publishDirectoryInfo = platformDirectory.GetDirectories("publish").SingleOrDefault();

                    if (publishDirectoryInfo is null)
                    {
                        _logger.WriteWarning(
                            $"The publish directory '{Path.Combine(platformDirectory.FullName, "publish")}' does not exist");
                        return ImmutableArray<ILRepackData>.Empty;
                    }

                    releasePlatformDirectory = publishDirectoryInfo;
                }
            }
            else
            {
                CsProjFile csProjFile = CsProjFile.LoadFrom(projectFile.FullName);

                const string targetFrameworkVersion = "TargetFrameworkVersion";

                MSBuildProperty msBuildProperty = csProjFile.BuildProject.PropertyGroups
                    .SelectMany(group =>
                        group.Properties.Where(
                            property => property.Name.Equals(
                                targetFrameworkVersion,
                                StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(msBuildProperty?.Value))
                {
                    throw new InvalidOperationException(
                        $"The CSProj file '{csProjFile.FileName}' does not contain a property '{targetFrameworkVersion}");
                }

                targetFrameworkVersionValue = NormalizeVersion(msBuildProperty.Value);

                releasePlatformDirectory = releaseDir;
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

            ImmutableArray<ILRepackData> mergeFiles =
                new[] { new ILRepackData(exe.FullName, dlls, configuration, platform, targetFrameworkVersionValue) }
                    .ToImmutableArray();

            return mergeFiles;
        }

        private string GetPlatform(FileInfo exe)
        {
            Assembly assembly = Assembly.LoadFile(Path.GetFullPath(exe.FullName));
            Module manifestModule = assembly.ManifestModule;
            manifestModule.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);

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
