using Zio;

namespace Arbor.Build.Core.Tools.MSBuild;

public class WebSolutionProject(
    FileEntry fullPath,
    string projectName,
    DirectoryEntry projectDirectory,
    MsBuildProject msbuildProject,
    NetFrameworkGeneration netFrameworkGeneration)
    : SolutionProject(fullPath, projectName, projectDirectory, msbuildProject, netFrameworkGeneration);