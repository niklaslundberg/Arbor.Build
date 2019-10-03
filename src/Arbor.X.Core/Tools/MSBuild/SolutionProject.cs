namespace Arbor.Build.Core.Tools.MSBuild
{
    public class SolutionProject
    {
        public SolutionProject(
            string fullPath,
            string projectName,
            string projectDirectory,
            MSBuildProject msbuildProject,
            NetFrameworkGeneration netFrameworkGeneration)
        {
            FullPath = fullPath;
            ProjectName = projectName;
            ProjectDirectory = projectDirectory;
            NetFrameworkGeneration = netFrameworkGeneration;
            Project = msbuildProject;
        }

        public string FullPath { get; }

        public string ProjectName { get; }

        public NetFrameworkGeneration NetFrameworkGeneration { get; }

        public string ProjectDirectory { get; }

        public MSBuildProject Project { get; }
    }
}
