using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public class MSBuildProject
    {
        private MSBuildProject(
            IReadOnlyList<MSBuildPropertyGroup> propertyGroups,
            string fileName,
            string projectName,
            string projectDirectory,
            ImmutableArray<ProjectType> projectTypes,
            Guid? projectId,
            DotNetSdk sdk,
            ImmutableArray<PackageReferenceElement> packageReferences)
        {
            PropertyGroups = propertyGroups.ToImmutableArray();
            FileName = fileName;
            ProjectName = projectName;
            ProjectDirectory = projectDirectory;
            ProjectTypes = projectTypes;
            ProjectId = projectId;
            Sdk = sdk;
            PackageReferences = packageReferences;
        }

        public ImmutableArray<MSBuildPropertyGroup> PropertyGroups { get; }

        public string FileName { get; }

        public string ProjectName { get; }

        public string ProjectDirectory { get; }

        public ImmutableArray<ProjectType> ProjectTypes { get; }

        public Guid? ProjectId { get; }

        public DotNetSdk Sdk { get; }

        public ImmutableArray<PackageReferenceElement> PackageReferences { get; }

        public static bool IsNetSdkProject([NotNull] FileInfo projectFile)
        {
            if (projectFile == null)
            {
                throw new ArgumentNullException(nameof(projectFile));
            }

            if (projectFile.FullName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                throw new InvalidOperationException(Resources.ThePathContainsInvalidCharacters);
            }

            if (projectFile.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException(Resources.ThePathContainsInvalidCharacters);
            }

            return File.ReadLines(projectFile.FullName)
                .Any(line => line.IndexOf("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static MSBuildProject LoadFrom(string projectFileFullName)
        {
            using var fs = new FileStream(projectFileFullName, FileMode.Open, FileAccess.Read);
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

            ImmutableArray<XElement> propertyGroups = project
                .Elements()
                .Where(e => e.Name.LocalName.Equals("PropertyGroup", StringComparison.Ordinal))
                .ToImmutableArray();

            XElement idElement = propertyGroups
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals("ProjectGuid", StringComparison.Ordinal));

            if (idElement?.Value != null)
            {
                if (Guid.TryParse(idElement.Value, out Guid id))
                {
                    projectId = id;
                }
            }

            foreach (XElement propertyGroup in propertyGroups)
            {
                ImmutableArray<MSBuildProperty> msBuildProperties = propertyGroup?
                                                                        .Elements()
                                                                        .Select(p =>
                                                                            new MSBuildProperty(p.Name.LocalName,
                                                                                p.Value))
                                                                        .ToImmutableArray()
                                                                    ?? ImmutableArray<MSBuildProperty>.Empty;

                msbuildPropertyGroups.Add(new MSBuildPropertyGroup(msBuildProperties));
            }

            string name = Path.GetFileNameWithoutExtension(projectFileFullName);

            var file = new FileInfo(projectFileFullName);

            XElement projectTypeGuidsElement = propertyGroups
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals("ProjectTypeGuids", StringComparison.Ordinal));

            ImmutableArray<ProjectType> projectTypes = ImmutableArray<ProjectType>.Empty;

            if (projectTypeGuidsElement != null)
            {
                projectTypes = projectTypeGuidsElement.Value.Split(';')
                    .Select(Guid.Parse)
                    .Select(guid => new ProjectType(guid))
                    .ToImmutableArray();
            }

            string sdkValue = project.Attribute("Sdk")?.Value;

            DotNetSdk sdk = DotNetSdk.ParseOrDefault(sdkValue);

            ImmutableArray<PackageReferenceElement> packageReferences = propertyGroups
                .Where(e => e.Name.LocalName.Equals("PackageReference", StringComparison.Ordinal))
                .Select(packageReference => new PackageReferenceElement(
                    packageReference.Attribute("Include")?.Value,
                    packageReference.Attribute("Version")?.Value))
                .Where(reference => reference.IsValid)
                .ToImmutableArray();

            return new MSBuildProject(msbuildPropertyGroups,
                projectFileFullName,
                name,
                file.Directory?.FullName,
                projectTypes,
                projectId,
                sdk,
                packageReferences);
        }

        public override string ToString() => $"{nameof(Properties)} [{PropertyGroups.SelectMany(g => g.Properties).Count()}]:{Environment.NewLine}{string.Join(Environment.NewLine, PropertyGroups.SelectMany(g => g.Properties).Select(p => "\t" + p.ToString()))}{Environment.NewLine}{nameof(FileName)}: {FileName}{Environment.NewLine}{nameof(ProjectName)}: {ProjectName}{Environment.NewLine}{nameof(ProjectDirectory)}: {ProjectDirectory}{nameof(ProjectTypes)}: {string.Join(", ", ProjectTypes.Select(t => t.ToString()))},{Environment.NewLine}{nameof(ProjectId)}: {ProjectId}{Environment.NewLine}{nameof(Sdk)}: {Sdk}{Environment.NewLine}{nameof(PackageReferences)} [{PackageReferences.Length}]:{Environment.NewLine} {string.Join(Environment.NewLine, PackageReferences.Select(r => r.ToString()))}";

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

        public string GetPropertyValue([NotNull] string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            List<MSBuildProperty> msBuildProperties = PropertyGroups
                .SelectMany(propertyGroup =>
                    propertyGroup.Properties.Where(property =>
                        property.Name.Equals(name, StringComparison.Ordinal)))
                .ToList();

            if (msBuildProperties.Count == 0)
            {
                return string.Empty;
            }

            if (msBuildProperties.Count > 1)
            {
                throw new InvalidOperationException($"Multiple MSBuild properties were found with name '{name}' in project file '{FileName}'");
            }

            return msBuildProperties[0].Value;
        }
    }
}
