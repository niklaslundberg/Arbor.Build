using System;

namespace Arbor.X.Core.Tools.MSBuild
{
    public class ProjectType
    {
        public static readonly ProjectType Mvc5 = new ProjectType(Guid.Parse("349c5851-65df-11da-9384-00065b846f21"));

        public static readonly ProjectType CSharp = new ProjectType(Guid.Parse("fae04ec0-301f-11d3-bf4b-00c04f79efbc"));

        public static readonly ProjectType SolutionFolder = new ProjectType(Guid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8"));

        public ProjectType(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Project type Guid cannot be empty Guid");
            }

            Id = id;
        }

        public Guid Id { get; }
    }
}
