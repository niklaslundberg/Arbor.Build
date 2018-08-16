namespace Arbor.Build.Core.Tools.MSBuild
{
    public class SolutionProject
    {
        public SolutionProject(
            string fullPath,
            string projectName,
            string projectDirectory,
            MSBuildProject msbuildProject,
            Framework framework)
        {
            FullPath = fullPath;
            ProjectName = projectName;
            ProjectDirectory = projectDirectory;
            Framework = framework;
            Project = msbuildProject;
        }

        public string FullPath { get; }

        public string ProjectName { get; }

        public Framework Framework { get; }

        public string ProjectDirectory { get; }

        public MSBuildProject Project { get; }
    }
}
