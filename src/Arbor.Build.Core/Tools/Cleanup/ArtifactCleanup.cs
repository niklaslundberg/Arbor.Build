﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Exceptions;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.FS;
using Arbor.Processing;
using JetBrains.Annotations;
using Serilog;
using Zio;

namespace Arbor.Build.Core.Tools.Cleanup;

[Priority(41)]
[UsedImplicitly]
public class ArtifactCleanup(IFileSystem fileSystem, BuildContext buildContext) : ITool
{
    private readonly BuildContext _buildContext = buildContext;

    public async Task<ExitCode> ExecuteAsync(
        ILogger logger,
        IReadOnlyCollection<IVariable> buildVariables,
        string[] args,
        CancellationToken cancellationToken)
    {
        bool cleanupBeforeBuildEnabled =
            buildVariables.GetBooleanByKey(
                WellKnownVariables.CleanupArtifactsBeforeBuildEnabled,
                true);

        if (!cleanupBeforeBuildEnabled)
        {
            logger.Verbose("Cleanup before build is disabled");
            return ExitCode.Success;
        }

        var artifactsPath = buildVariables.Require(WellKnownVariables.Artifacts).GetValueOrThrow().ParseAsPath();

        var artifactsDirectory = new DirectoryEntry(fileSystem, artifactsPath);

        if (!artifactsDirectory.Exists)
        {
            return ExitCode.Success;
        }

        const int maxAttempts = 5;

        int attemptCount = 1;

        bool cleanupSucceeded = false;

        while (attemptCount <= maxAttempts && !cleanupSucceeded)
        {
            bool result = TryCleanup(logger, artifactsDirectory, attemptCount == maxAttempts);

            if (result)
            {
                logger.Verbose("Cleanup succeeded on attempt {AttemptCount}", attemptCount);
                cleanupSucceeded = true;
            }
            else
            {
                logger.Verbose(
                    "Attempt {AttemptCount} of {MaxAttempts} failed, could not cleanup the artifacts folder, retrying",
                    attemptCount,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }

            attemptCount++;
        }

        return ExitCode.Success;
    }

    private static bool TryCleanup(
        ILogger logger,
        DirectoryEntry artifactsDirectory,
        bool throwExceptionOnFailure = false)
    {
        try
        {
            DoCleanup(logger, artifactsDirectory);
        }
        catch (Exception ex)
        {
            if (ex.IsFatal())
            {
                throw;
            }

            if (throwExceptionOnFailure)
            {
                throw;
            }

            return false;
        }

        return true;
    }

    private static void DoCleanup(ILogger logger, DirectoryEntry artifactsDirectory)
    {
        logger.Information("Artifact cleanup is enabled, removing all files and folders in '{FullName}'",
            artifactsDirectory.FileSystem.ConvertPathToInternal(artifactsDirectory.Path));

        artifactsDirectory.DeleteIfExists();
        artifactsDirectory.EnsureExists();
    }
}