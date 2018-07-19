namespace Arbor.X.Core.Tools.MSBuild
{
    public class WebSolutionProject : SolutionProject
    {
        public WebSolutionProject(
            string fullPath,
            string projectName,
            string projectDirectory,
            MSBuildProject msbuildProject,
            Framework framework) : base(fullPath, projectName, projectDirectory, msbuildProject, framework)
        {
        }
    }
}
