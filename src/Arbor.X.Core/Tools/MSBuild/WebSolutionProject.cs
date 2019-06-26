namespace Arbor.Build.Core.Tools.MSBuild
{
    public class WebSolutionProject : SolutionProject
    {
        public WebSolutionProject(
            string fullPath,
            string projectName,
            string projectDirectory,
            MSBuildProject msbuildProject,
            NetFrameworkGeneration netFrameworkGeneration) : base(fullPath, projectName, projectDirectory, msbuildProject, netFrameworkGeneration)
        {
        }
    }
}
