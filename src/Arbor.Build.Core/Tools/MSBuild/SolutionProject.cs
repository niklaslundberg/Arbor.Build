using Zio;

namespace Arbor.Build.Core.Tools.MSBuild;

public class SolutionProject(
    FileEntry fullPath,
    string projectName,
    DirectoryEntry projectDirectory,
    MsBuildProject msbuildProject,
    NetFrameworkGeneration netFrameworkGeneration)
{
    public FileEntry FullPath { get; } = fullPath;

    public string ProjectName { get; } = projectName;

    public NetFrameworkGeneration NetFrameworkGeneration { get; } = netFrameworkGeneration;

    public DirectoryEntry ProjectDirectory { get; } = projectDirectory;

    public MsBuildProject Project { get; } = msbuildProject;

    public override string ToString() => ProjectName;
}