using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.GenericExtensions;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.ProcessUtils;

using JetBrains.Annotations;

using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace Arbor.X.Core.Tools.ILMerge
{
    [Priority(620)]
    [UsedImplicitly]
    // ReSharper disable once InconsistentNaming
    public class ILMerger : ITool
    {
        string _artifactsPath;
        string _ilMergeExePath;
        ILogger _logger;

        public async Task<ExitCode> ExecuteAsync(ILogger logger, IReadOnlyCollection<IVariable> buildVariables, CancellationToken cancellationToken)
        {
            _logger = logger;
            _ilMergeExePath =
                buildVariables.Require(WellKnownVariables.ExternalTools_ILMerge_ExePath).ThrowIfEmptyValue().Value;
            _artifactsPath =
                buildVariables.Require(WellKnownVariables.Artifacts).ThrowIfEmptyValue().Value;

            var sourceRoot = buildVariables.Require(WellKnownVariables.SourceRoot).ThrowIfEmptyValue().Value;

            var sourceRootDirectory = new DirectoryInfo(sourceRoot);
            var csharpProjectFiles = sourceRootDirectory.GetFiles("*.csproj", SearchOption.AllDirectories);

            List<FileInfo> ilMergeProjects = csharpProjectFiles.Where(IsILMergeEnabledInProjectFile).ToList();

            var merges = string.Join(Environment.NewLine, ilMergeProjects.Select(item => item.FullName));

            logger.Write($"Found {ilMergeProjects.Count} projects marked for ILMerge:{Environment.NewLine}{merges}");

            IReadOnlyCollection<ILMergeData> mergeDatas = ilMergeProjects.SelectMany(GetIlMergeFiles).ToReadOnlyCollection();

            foreach (ILMergeData mergeData in mergeDatas)
            {
                var fileInfo = new FileInfo(mergeData.Exe);
                var ilMergedDirectoryPath = Path.Combine(_artifactsPath, "ILMerged", mergeData.Platform,
                    mergeData.Configuration);
                var ilMergedDirectory = new DirectoryInfo(ilMergedDirectoryPath).EnsureExists();

                var ilMergedPath = Path.Combine(ilMergedDirectory.FullName, fileInfo.Name);
                var arguments = new List<string> {"/target:exe", "/out:" + ilMergedPath, fileInfo.FullName};
                arguments.AddRange(mergeData.Dlls.Select(dll => dll.FullName));

                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                string dotNetVersion = "4.5.1";

                string referenceAssemblyDirectory =
                    $@"{programFiles}\Reference Assemblies\Microsoft\Framework\.NETFramework\v" + dotNetVersion;

                if (!Directory.Exists(referenceAssemblyDirectory))
                {
                    logger.WriteError(
                        $"Could not ILMerge, the reference assembly directory {referenceAssemblyDirectory} does not exist, currently only .NET v{dotNetVersion} is supported");

                    return ExitCode.Failure;
                }

                arguments.Add(
                    $@"/targetplatform:v4,{referenceAssemblyDirectory}");

                var result =
                    await
                        ProcessRunner.ExecuteAsync(_ilMergeExePath, arguments: arguments, standardOutLog: logger.Write,
                            toolAction: logger.Write, standardErrorAction: logger.WriteError, cancellationToken: cancellationToken);

                if (!result.IsSuccess)
                {
                    logger.WriteError("Could not ILMerge " + fileInfo.FullName);
                    return result;
                }

                logger.Write($"ILMerged result: {ilMergedPath}");
            }

            return ExitCode.Success;
        }

        IEnumerable<ILMergeData> GetIlMergeFiles(FileInfo projectFile)
        {
// ReSharper disable PossibleNullReferenceException
            var binDirectory = projectFile.Directory.GetDirectories("bin").SingleOrDefault();
// ReSharper restore PossibleNullReferenceException

            if (binDirectory == null)
            {
                yield break;
            }

            string configuration = "release"; //TODO support ilmerge for debug

            var releaseDir = binDirectory.GetDirectories(configuration).SingleOrDefault();

            if (releaseDir == null)
            {
                _logger.WriteWarning(
                    $"A release directory '{Path.Combine(binDirectory.FullName, configuration)}' was not found");
                yield break;
            }

            var exes = releaseDir
                .EnumerateFiles("*.exe")
                .Where(FileIsStandAloneExe)
                .ToList();

            if (exes.Count != 1)
            {
                throw new InvalidOperationException("Only one exe can be ILMerged");
            }

            var exe = exes.Single();

            string platform = GetPlatform(exe);

            var dlls =
                releaseDir.EnumerateFiles("*.dll")
                    .Where(FileIsStandAloneExe);


            yield return new ILMergeData(exe.FullName, dlls, configuration, platform);
        }

        private static bool FileIsStandAloneExe(FileInfo file)
        {
            return file.Name.IndexOf(".vshost.", StringComparison.InvariantCultureIgnoreCase) < 0;
        }

        string GetPlatform(FileInfo exe)
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

        bool IsILMergeEnabledInProjectFile(FileInfo file)
        {
            using (var fs = file.OpenRead())
            {
                using (var streamReader = new StreamReader(fs))
                {
                    while (streamReader.Peek() >= 0)
                    {
                        var line = streamReader.ReadLine();

                        if (
                            line?.IndexOf("<ILMergeExe>true</ILMergeExe>",
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
