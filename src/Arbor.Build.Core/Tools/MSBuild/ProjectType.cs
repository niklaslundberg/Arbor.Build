using System;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public sealed class ProjectType : IEquatable<ProjectType>
    {
        public static readonly ProjectType Mvc5 = new(Guid.Parse("349c5851-65df-11da-9384-00065b846f21"));

        public static readonly ProjectType CSharp = new(Guid.Parse("fae04ec0-301f-11d3-bf4b-00c04f79efbc"));

        public static readonly ProjectType SolutionFolder =
            new(Guid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8"));

        public ProjectType(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Project type Guid cannot be empty Guid");
            }

            Id = id;
        }

        public Guid Id { get; }

        public static bool operator ==(ProjectType left, ProjectType right) => Equals(left, right);

        public static bool operator !=(ProjectType left, ProjectType right) => !Equals(left, right);

        public bool Equals(ProjectType? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ProjectType)obj);
        }

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => $"{nameof(Id)}: {Id}";
    }
}
