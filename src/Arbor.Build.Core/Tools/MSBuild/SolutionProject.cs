using Zio;

namespace Arbor.Build.Core.Tools.MSBuild;

public class SolutionProject
{
    public SolutionProject(
        FileEntry fullPath,
        string projectName,
        DirectoryEntry projectDirectory,
        MsBuildProject msbuildProject,
        NetFrameworkGeneration netFrameworkGeneration)
    {
        FullPath = fullPath;
        ProjectName = projectName;
        ProjectDirectory = projectDirectory;
        NetFrameworkGeneration = netFrameworkGeneration;
        Project = msbuildProject;
    }

    public FileEntry FullPath { get; }

    public string ProjectName { get; }

    public NetFrameworkGeneration NetFrameworkGeneration { get; }

    public DirectoryEntry ProjectDirectory { get; }

    public MsBuildProject Project { get; }

    public override string ToString() => ProjectName;
}