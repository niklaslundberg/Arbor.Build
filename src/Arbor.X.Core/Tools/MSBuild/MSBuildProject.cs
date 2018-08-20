using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public sealed class DotNetSdk : IEquatable<DotNetSdk>
    {
        public bool Equals(DotNetSdk other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(SdkName, other.SdkName, StringComparison.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is DotNetSdk sdk && Equals(sdk);
        }

        public override int GetHashCode()
        {
            return SdkName.GetHashCode(StringComparison.Ordinal);
        }

        public static bool operator ==(DotNetSdk left, DotNetSdk right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DotNetSdk left, DotNetSdk right)
        {
            return !Equals(left, right);
        }

        public static readonly DotNetSdk DotnetWeb = new DotNetSdk("Microsoft.NET.Sdk.Web");
        public static readonly DotNetSdk Dotnet = new DotNetSdk("Microsoft.NET.Sdk");
        public static readonly DotNetSdk None = new DotNetSdk("N/A");

        private static readonly Lazy<ImmutableArray<DotNetSdk>> _LazyAll =
            new Lazy<ImmutableArray<DotNetSdk>>(() => new[]
            {
                None,
                Dotnet,
                DotnetWeb,

            }.ToImmutableArray());

        public static ImmutableArray<DotNetSdk> All => _LazyAll.Value;

        public string SdkName { get; }

        private DotNetSdk([NotNull] string sdkName)
        {
            if (string.IsNullOrWhiteSpace(sdkName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sdkName));
            }

            SdkName = sdkName;
        }

        public static DotNetSdk ParseOrDefault(string sdkValue)
        {
            if (string.IsNullOrWhiteSpace(sdkValue))
            {
                return None;
            }

            return All.SingleOrDefault(sdk => sdk.SdkName.Equals(sdkValue, StringComparison.Ordinal));
        }
    }

    public class MSBuildProject
    {
        private MSBuildProject(
            IReadOnlyList<MSBuildPropertyGroup> propertyGroups,
            string fileName,
            string projectName,
            string projectDirectory,
            ImmutableArray<ProjectType> projectTypes,
            Guid? projectId,
            DotNetSdk sdk)
        {
            PropertyGroups = propertyGroups.ToImmutableArray();
            FileName = fileName;
            ProjectName = projectName;
            ProjectDirectory = projectDirectory;
            ProjectTypes = projectTypes;
            ProjectId = projectId;
            Sdk = sdk;
        }

        public ImmutableArray<MSBuildPropertyGroup> PropertyGroups { get; }

        public string FileName { get; }

        public string ProjectName { get; }

        public string ProjectDirectory { get; }

        public ImmutableArray<ProjectType> ProjectTypes { get; }

        public Guid? ProjectId { get; }

        public DotNetSdk Sdk { get; }

        public static bool IsNetSdkProject([NotNull] FileInfo projectFile)
        {
            if (projectFile == null)
            {
                throw new ArgumentNullException(nameof(projectFile));
            }

            if (projectFile.FullName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new InvalidOperationException("The temp path contains invalid characters");
            }

            if (projectFile.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("The temp path contains invalid characters");
            }

            return File.ReadLines(projectFile.FullName)
                .Any(line => line.IndexOf("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static MSBuildProject LoadFrom(string projectFileFullName)
        {
            using (var fs = new FileStream(projectFileFullName, FileMode.Open, FileAccess.Read))
            {
                var msbuildPropertyGroups = new List<MSBuildPropertyGroup>();

                Guid? projectId = default;

                XDocument document = XDocument.Load(fs);

                const string projectElementName = "Project";

                XElement project = document.Elements().SingleOrDefault(element =>
                    element.Name.LocalName.Equals(projectElementName, StringComparison.Ordinal));

                if (project is null)
                {
                    throw new InvalidOperationException(
                        $"Could not find element <{projectElementName}> in file '{projectFileFullName}'");
                }

                ImmutableArray<XElement> propertyGroups = project.Elements("PropertyGroup").ToImmutableArray();

                XElement idElement = propertyGroups.Elements("ProjectGuid").FirstOrDefault();

                if (Guid.TryParse(idElement?.Value, out Guid id))
                {
                    projectId = id;
                }

                foreach (XElement propertyGroup in propertyGroups)
                {
                    ImmutableArray<MSBuildProperty> msBuildProperties = propertyGroup?.Elements()
                                                                            .Select(p =>
                                                                                new MSBuildProperty(p.Name.LocalName,
                                                                                    p.Value))
                                                                            .ToImmutableArray()
                                                                        ?? ImmutableArray<MSBuildProperty>.Empty;

                    msbuildPropertyGroups.Add(new MSBuildPropertyGroup(msBuildProperties));
                }

                string name = Path.GetFileNameWithoutExtension(projectFileFullName);

                var file = new FileInfo(projectFileFullName);

                ImmutableArray<ProjectType> projectTypes = propertyGroups
                                                               .Elements("ProjectTypeGuids")
                                                               .FirstOrDefault()?.Value.Split(';')
                                                               .Select(Guid.Parse)
                                                               .Select(guid => new ProjectType(guid))
                                                               .ToImmutableArray()
                                                           ?? ImmutableArray<ProjectType>.Empty;

                string sdkValue = project.Attribute("Sdk")?.Value;

               DotNetSdk sdk = DotNetSdk.ParseOrDefault(sdkValue);

                return new MSBuildProject(msbuildPropertyGroups,
                    projectFileFullName,
                    name,
                    file.Directory?.FullName,
                    projectTypes,
                    projectId,
                    sdk);
            }
        }

        public bool HasPropertyWithValue([NotNull] string name, [NotNull] string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return PropertyGroups.Any(propertyGroup => propertyGroup.Properties.Any(property =>
                property.Name.Equals(name, StringComparison.Ordinal) &&
                value.Equals(property.Value, StringComparison.Ordinal)));
        }
    }
}
