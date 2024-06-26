﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Cleanup;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.DotNet;

[UsedImplicitly]
public class DotNetEnvironmentVariableProvider(IEnvironmentVariables environmentVariables, IFileSystem fileSystem)
    : IVariableProvider
{
    public int Order => VariableProviderOrder.Ignored;

    public async Task<IReadOnlyCollection<IVariable>> GetBuildVariablesAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        CancellationToken cancellationToken)
    {
        UPath? dotNetExePath =
            buildVariables.GetVariableValueOrDefault(WellKnownVariables.DotNetExePath)?.ParseAsPath();

        if (dotNetExePath.HasValue && dotNetExePath.Value != UPath.Empty)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(dotNetExePath?.FullName))
        {
            var sb = new List<string>(10);

            var winDir = environmentVariables.GetEnvironmentVariable("WINDIR")?.ParseAsPath();

            if (winDir is null)
            {
                logger.Warning("Error finding Windows directory");
                return [];
            }

            var whereExePath = UPath.Combine(winDir.Value, "System32", "where.exe");

            ExitCode exitCode = await Processing.ProcessRunner.ExecuteProcessAsync(
                fileSystem.ConvertPathToInternal(whereExePath),
                arguments: new[] { "dotnet.exe" },
                standardOutLog: (message, _) => sb.Add(message),
                cancellationToken: cancellationToken);

            if (!exitCode.IsSuccess)
            {
                logger.Error("Failed to find dotnet.exe with where.exe");
            }

            dotNetExePath =
                sb.FirstOrDefault(item => item.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))?.Trim().ParseAsPath();
        }
        else if (!fileSystem.FileExists(dotNetExePath.Value))
        {
            logger.Warning(
                "The specified path to dotnet.exe is from variable '{DotNetExePath}' is set to '{DotNetExePath1}' but the file does not exist",
                WellKnownVariables.DotNetExePath,
                fileSystem.ConvertPathToInternal(dotNetExePath.Value));
            return [];
        }

        return new IVariable[] { new BuildVariable(WellKnownVariables.DotNetExePath, string.IsNullOrWhiteSpace(dotNetExePath?.FullName) ? "" : fileSystem.ConvertPathToInternal(dotNetExePath.Value)) }
            .ToImmutableArray();
    }
}