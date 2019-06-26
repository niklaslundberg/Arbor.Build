using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
{
    internal class Solution
    {
        public Solution(ImmutableArray<SolutionProject> projects)
        {
            Projects = projects;
        }

        public ImmutableArray<SolutionProject> Projects { get; }

        public static Solution LoadFrom([NotNull] string solutionFileFullName)
        {
            if (string.IsNullOrWhiteSpace(solutionFileFullName))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrWhitespace, nameof(solutionFileFullName));
            }

            if (!File.Exists(solutionFileFullName))
            {
                throw new ArgumentException($"The file '{solutionFileFullName}' does not exists");
            }

            string[] lines = File.ReadAllLines(solutionFileFullName);

            var fileInfo = new FileInfo(solutionFileFullName);

            return new Solution(lines
                .Select(line => GetProject(line, fileInfo))
                .Where(item => item != null)
                .ToImmutableArray());
        }

        private static SolutionProject GetProject(string line, FileInfo fileInfo)
        {
            //Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "NCinema.Web.IisHost", "NCinema.Web.IisHost\NCinema.Web.IisHost.csproj", "{04854B5C-247C-4F59-834D-9ACF5048F29C}"

            if (!line.StartsWith("Project(\""))
            {
                return null;
            }

            if (line.Length < 49)
            {
                return null;
            }

            string projectFile = line.Split(',').Skip(1).FirstOrDefault()?.Trim().Trim('\"');

            string typeId = line.Substring(10, 36);

            if (!Guid.TryParse(typeId, out Guid idGuid))
            {
                return null;
            }

            if (idGuid == ProjectType.SolutionFolder.Id)
            {
                return null;
            }

            string projectFullPath = Path.Combine(fileInfo.Directory.FullName, projectFile);

            MSBuildProject msBuildProject = MSBuildProject.LoadFrom(projectFullPath);

            NetFrameworkGeneration netFrameworkGeneration = MSBuildProject.IsNetSdkProject(new FileInfo(projectFullPath))
                ? NetFrameworkGeneration.NetCoreApp
                : NetFrameworkGeneration.NetFramework;

            return new SolutionProject(projectFullPath,
                msBuildProject.ProjectName,
                msBuildProject.ProjectDirectory,
                msBuildProject,
                netFrameworkGeneration);
        }
    }
}
