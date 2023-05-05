using System;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Logging;
using Arbor.FS;
using Arbor.Processing;
using Serilog;
using Serilog.Core;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Bootstrapper;

internal static class Program
{
    private static PhysicalFileSystem _physicalFileSystem = null!;
    private static WindowsFs _fileSystem = null!;

    private static Task<int> Main(string[] args) => RunAsync(args);

    public static async Task<int> RunAsync(string[] args, IEnvironmentVariables? environmentVariables = default, IFileSystem? fileSystem = default)
    {
        environmentVariables ??= new DefaultEnvironmentVariables();

        Logger logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _physicalFileSystem = new PhysicalFileSystem();
        _fileSystem = new WindowsFs(_physicalFileSystem);
        var bootstrapper = new Core.Bootstrapper.AppBootstrapper(logger, environmentVariables, fileSystem ?? _fileSystem);

        ExitCode exitCode = await bootstrapper.StartAsync(args).ConfigureAwait(false);

        if (logger is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _physicalFileSystem.Dispose();
        _fileSystem.Dispose();

        return exitCode.Code;
    }
}