﻿using System.Threading.Tasks;
using Arbor.Build.Core.BuildApp;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Logging;
using Arbor.Build.Core.Tools.EnvironmentVariables;
using Arbor.FS;
using Arbor.Processing;
using Serilog;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build;

internal static class Program
{
    private static BuildApplication? _app;
    private static WindowsFs? _fileSystem;

    private static Task<int> Main(string[] args) => RunAsync(args);

    public static async Task<int> RunAsync(
        string[]? args,
        IEnvironmentVariables? environmentVariables = default,
        ISpecialFolders? specialFolders = default,
        IFileSystem? fileSystem = default)
    {
        args ??= [];
        environmentVariables ??= new DefaultEnvironmentVariables();
        specialFolders ??= SpecialFolders.Default;

        ILogger logger = LoggerInitialization.InitializeLogging(args, environmentVariables);

        Log.Logger = logger;

        using var physicalFileSystem = new PhysicalFileSystem();
        using (_fileSystem = new WindowsFs(physicalFileSystem))
        {
            _app = new BuildApplication(logger, environmentVariables, specialFolders, fileSystem ?? _fileSystem);

            using (_app)
            {
                ExitCode exitCode = await _app.RunAsync(args);

                await Log.CloseAndFlushAsync();

                return exitCode.Code;
            }
        }
    }
}