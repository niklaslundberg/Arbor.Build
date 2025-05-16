using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Zio;

namespace Arbor.Build.Core.Tools.MSBuild;

internal class Solution(FileEntry fullPath, ImmutableArray<SolutionProject> projects)
{
    public string Name { get; } = fullPath.Name;

    public FileEntry FullPath { get; } = fullPath;

    public override string ToString() => Name;

    public ImmutableArray<SolutionProject> Projects { get; } = projects;

    public static async Task<Solution> LoadFrom(FileEntry solutionFileFullName, CancellationToken cancellationToken = default) =>
        solutionFileFullName.Path.GetExtensionWithDot()?.Equals(".slnx") == true
            ? await LoadFromSlnx(solutionFileFullName, cancellationToken)
            : await LoadFromSln(solutionFileFullName, cancellationToken);

    private static async Task<Solution> LoadFromSlnx(FileEntry solutionFileFullName, CancellationToken cancellationToken)
    {
        await using var stream = solutionFileFullName.Open(FileMode.Open, FileAccess.Read);
        var solution = await SolutionSerializers.SlnXml.OpenAsync(stream, cancellationToken);

        var solutionProjects = new List<SolutionProject>();

        foreach (var project in solution.SolutionProjects)
        {
            solutionProjects.Add(await GetFromProject(project, solutionFileFullName));
        }

        return new Solution(solutionFileFullName, [.. solutionProjects]);
    }

    private static async Task<SolutionProject> GetFromProject(SolutionProjectModel projectModel, FileEntry solutionFileFullName)
    {
        var fullPath = solutionFileFullName.Directory.Path / projectModel.FilePath;

        var projectFile = solutionFileFullName.FileSystem.GetFileEntry(fullPath);

        MsBuildProject msBuildProject = await MsBuildProject.LoadFrom(projectFile);

        NetFrameworkGeneration netFrameworkGeneration = await MsBuildProject.IsNetSdkProject(projectFile)
            ? NetFrameworkGeneration.NetCoreApp
            : NetFrameworkGeneration.NetFramework;

        return new SolutionProject(projectFile,
            msBuildProject.ProjectName,
            msBuildProject.ProjectDirectory,
            msBuildProject,
            netFrameworkGeneration);
    }

    private static async Task<Solution> LoadFromSln(FileEntry solutionFileFullName, CancellationToken cancellationToken)
    {
        var stream = solutionFileFullName.Open(FileMode.Open, FileAccess.Read);

        var lines = await stream.ReadAllLinesAsync(cancellationToken: cancellationToken);

        var projects = new List<SolutionProject>();

        foreach (string line in lines)
        {
            var project = await GetProjectInSln(line, solutionFileFullName, cancellationToken);

            if (project is { })
            {
                projects.Add(project);
            }
        }

        return new Solution(solutionFileFullName, [.. projects]);
    }

    private static async Task<SolutionProject?> GetProjectInSln(string line, FileEntry fileEntry, CancellationToken cancellationToken)
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
        MsBuildProject msBuildProject = await MsBuildProject.LoadFrom(projectFileFullName, cancellationToken);

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