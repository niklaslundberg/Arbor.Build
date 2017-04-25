using FubuCsProjFile.MSBuild;

namespace Arbor.X.Core.Tools.MSBuild
{
    public class WebSolutionProject
    {
        public WebSolutionProject(string fullPath, string projectName, string projectDirectory,
            MSBuildProject msbuildProject, Framework framework)
        {
            FullPath = fullPath;
            ProjectName = projectName;
            ProjectDirectory = projectDirectory;
            Framework = framework;
            BuildProject = msbuildProject;
        }

        public string FullPath { get; }

        public string ProjectName { get; }

        public Framework Framework { get; }

        public string ProjectDirectory { get; }

        public MSBuildProject BuildProject { get; }
    }
}
