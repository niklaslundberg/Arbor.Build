﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arbor.FS;
using Zio;

namespace Arbor.Build.Core.Tools.MSBuild;

internal class Solution(FileEntry fullPath, ImmutableArray<SolutionProject> projects)
{
    public string Name { get; } = fullPath.Name;

    public FileEntry FullPath { get; } = fullPath;

    public override string ToString() => Name;

    public ImmutableArray<SolutionProject> Projects { get; } = projects;

    public static async Task<Solution> LoadFrom(FileEntry solutionFileFullName)
    {
        var stream = solutionFileFullName.Open(FileMode.Open, FileAccess.Read);

        var lines = await stream.ReadAllLinesAsync();

        var projects = new List<SolutionProject>();

        foreach (string line in lines)
        {
            var project = await GetProject(line, solutionFileFullName);

            if (project is {})
            {
                projects.Add(project);
            }
        }

        return new Solution(solutionFileFullName, projects.ToImmutableArray());
    }

    private static async Task<SolutionProject?> GetProject(string line, FileEntry fileEntry)
    {
        //Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "NCinema.Web.IisHost", "NCinema.Web.IisHost\NCinema.Web.IisHost.csproj", "{04854B5C-247C-4F59-834D-9ACF5048F29C}"

        if (!line.StartsWith("Project(\"", StringComparison.Ordinal))
        {
            return null;
        }

        if (line.Length < 49)
        {
            return null;
        }

        string? projectFile = line.Split(',').Skip(1).FirstOrDefault()?.Trim().Trim('\"');

        if (string.IsNullOrWhiteSpace(projectFile))
        {
            return null;
        }

        string typeId = line.Substring(10, 36);

        if (!Guid.TryParse(typeId, out Guid idGuid))
        {
            return null;
        }

        if (idGuid == ProjectType.SolutionFolder.Id)
        {
            return null;
        }

        if (fileEntry.Directory is null)
        {
            throw new InvalidOperationException("Directory property is null");
        }

        var projectFullPath = UPath.Combine(fileEntry.Directory.Path, projectFile);

        var projectFileFullName = fileEntry.FileSystem.GetFileEntry(projectFullPath);
        MsBuildProject msBuildProject = await MsBuildProject.LoadFrom(projectFileFullName);

        NetFrameworkGeneration netFrameworkGeneration = await MsBuildProject.IsNetSdkProject(projectFileFullName)
            ? NetFrameworkGeneration.NetCoreApp
            : NetFrameworkGeneration.NetFramework;

        return new SolutionProject(projectFileFullName,
            msBuildProject.ProjectName,
            msBuildProject.ProjectDirectory,
            msBuildProject,
            netFrameworkGeneration);
    }
}