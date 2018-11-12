using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.GenericExtensions.Boolean;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.Defensive.Collections;
using Arbor.Processing;
using Arbor.Processing.Core;
using JetBrains.Annotations;
using Serilog;

// ReSharper disable once InconsistentNaming
namespace Arbor.Build.Core.Tools.ILRepack
{
    [Priority(620)]
    [UsedImplicitly]
    public class ILRepacker : ITool
    {
        private string _artifactsPath;
        private string _ilRepackExePath;
        private ILogger _logger;

        public async Task<ExitCode> ExecuteAsync(
            ILogger logger,
            IReadOnlyCollection<IVariable> buildVariables,
            CancellationToken cancellationToken)
        {
            _logger = logger;

            bool parseResult = buildVariables
                .GetVariableValueOrDefault(WellKnownVariables.ExternalTools_ILRepack_Enabled, "false")
                .ParseOrDefault(false);

            if (!parseResult)
            {
                _logger.Information(
                    "ILRepack is disabled, to enable it, set the flag {ExternalTools_ILRepack_Enabled} to true",
                    WellKnownVariables.ExternalTools_ILRepack_Enabled);
                return ExitCode.Success;
            }

            _ilRepackExePath =
                buildVariables.Require(WellKnownVariables.ExternalTools_ILRepack_ExePath).ThrowIfEmptyValue().Value;

            string customILRepackPath =
                buildVariables.GetVariableValueOrDefault(
                    WellKnownVariables.ExternalTools_ILRepack_Custom_ExePath,
                    string.Empty);

            if (!string.IsNullOrWhiteSpace(customILRepackPath) && File.Exists(customILRepackPath))
            {
                logger.Information("Using custom path for ILRepack: '{CustomILRepackPath}'", customILRepackPath);
                _ilRepackExePath = customILRepackPath;
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

            List<FileInfo> ilMergeProjects = csharpProjectFiles.Where(IsILMergeEnabledInProjectFile).ToList();

            string merges = string.Join(Environment.NewLine, ilMergeProjects.Select(item => item.FullName));

            logger.Information("Found {Count} projects marked for ILMerge:{NewLine}{Merges}",
                ilMergeProjects.Count,
                Environment.NewLine,
                merges);

            IReadOnlyCollection<ILRepackData> mergeDatas = ilMergeProjects.SelectMany(GetIlMergeFiles)
                .ToReadOnlyCollection();

            foreach (ILRepackData repackData in mergeDatas)
            {
                var fileInfo = new FileInfo(repackData.Exe);

                string ilMergedDirectoryPath = Path.Combine(
                    _artifactsPath,
                    "ILMerged",
                    repackData.Platform,
                    repackData.Configuration);

                DirectoryInfo ilMergedDirectory = new DirectoryInfo(ilMergedDirectoryPath).EnsureExists();

                string ilMergedPath = Path.Combine(ilMergedDirectory.FullName, fileInfo.Name);
                var arguments = new List<string>
                {
                    "/target:exe",
                    $"/out:{ilMergedPath}",
                    $"/Lib:{fileInfo.Directory.FullName}",
                    "/verbose",
                    fileInfo.FullName
                };

                arguments.AddRange(repackData.Dlls.Select(dll => dll.FullName));

                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                string dotNetVersion = repackData.TargetFramework;

                string referenceAssemblyDirectory =
                    $"{$@"{programFiles}\Reference Assemblies\Microsoft\Framework\.NETFramework\v"}{dotNetVersion}";

                if (!Directory.Exists(referenceAssemblyDirectory))
                {
                    logger.Error(
                        "Could not ILMerge, the reference assembly directory {ReferenceAssemblyDirectory} does not exist, currently only .NET v{DotNetVersion} is supported",
                        referenceAssemblyDirectory,
                        dotNetVersion);

                    return ExitCode.Failure;
                }

                arguments.Add(
                    $@"/targetplatform:v4,{referenceAssemblyDirectory}");

                ExitCode result =
                    await
                        ProcessRunner.ExecuteAsync(
                            _ilRepackExePath,
                            arguments: arguments,
                            standardOutLog: logger.Information,
                            toolAction: logger.Information,
                            standardErrorAction: logger.Error,
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    logger.Error("Could not ILRepack '{FullName}'", fileInfo.FullName);
                    return result;
                }

                logger.Information("ILMerged result: {IlMergedPath}", ilMergedPath);
            }

            return ExitCode.Success;
        }

        private static bool FileIsStandAloneExe(FileInfo file)
        {
            return file.Name.IndexOf(".vshost.", StringComparison.InvariantCultureIgnoreCase) < 0;
        }

        private IEnumerable<ILRepackData> GetIlMergeFiles(FileInfo projectFile)
        {
// ReSharper disable PossibleNullReferenceException
            DirectoryInfo binDirectory = projectFile.Directory.GetDirectories("bin").SingleOrDefault();

            // ReSharper restore PossibleNullReferenceException

            if (binDirectory == null)
            {
                yield break;
            }

            const string configuration = "release"; // TODO support ilmerge for debug

            DirectoryInfo releaseDir = binDirectory.GetDirectories(configuration).SingleOrDefault();

            if (releaseDir == null)
            {
                _logger.Warning("A release directory '{V}' was not found",
                    Path.Combine(binDirectory.FullName, configuration));
                yield break;
            }

            MSBuildProject csProjFile = MSBuildProject.LoadFrom(projectFile.FullName);

            const string targetFrameworkVersion = "TargetFrameworkVersion";

            MSBuildProperty msBuildProperty = csProjFile.PropertyGroups.SelectMany(
                    group =>
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

            List<FileInfo> exes = releaseDir
                .EnumerateFiles("*.exe")
                .Where(FileIsStandAloneExe)
                .ToList();

            if (exes.Count != 1)
            {
                throw new InvalidOperationException("Only one exe can be ILMerged");
            }

            FileInfo exe = exes.Single();

            string platform = GetPlatform(exe);

            ImmutableArray<FileInfo> dlls = releaseDir
                .EnumerateFiles("*.dll")
                .Where(FileIsStandAloneExe)
                .ToImmutableArray();

            string NormalizeVersion(string value)
            {
                if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                {
                    return value.Substring(1);
                }

                return value;
            }

            string targetFrameworkVersionValue = NormalizeVersion(msBuildProperty.Value);

            yield return new ILRepackData(exe.FullName, dlls, configuration, platform, targetFrameworkVersionValue);
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

        private bool IsILMergeEnabledInProjectFile(FileInfo file)
        {
            using (FileStream fs = file.OpenRead())
            {
                using (var streamReader = new StreamReader(fs))
                {
                    while (streamReader.Peek() >= 0)
                    {
                        string line = streamReader.ReadLine();

                        if (
                            line?.IndexOf(
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
