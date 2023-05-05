using Zio;

namespace Arbor.Build.Core.Tools.MSBuild;

public class WebSolutionProject : SolutionProject
{
    public WebSolutionProject(
        FileEntry fullPath,
        string projectName,
        DirectoryEntry projectDirectory,
        MsBuildProject msbuildProject,
        NetFrameworkGeneration netFrameworkGeneration) : base(fullPath, projectName, projectDirectory, msbuildProject, netFrameworkGeneration)
    {
    }
}