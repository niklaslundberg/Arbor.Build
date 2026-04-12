using System;
using System.Linq;

namespace Arbor.Build.Core.Tools.MSBuild;

public static class SolutionProjectExtensions
{
    extension(SolutionProject project)
    {
        public bool PublishEnabled()
        {
            if (project.Project.Sdk is null)
            {
                return false;
            }

            if (project.Project.Sdk == DotNetSdk.DotnetWeb)
            {
                return true;
            }

            if (project.NetFrameworkGeneration == NetFrameworkGeneration.NetFramework)
            {
                return false;
            }

            bool hasTestSdkReference = project.Project.PackageReferences.Any(reference =>
                reference.Package is { } packageName &&
                packageName.Equals(DotNetSdk.Test.SdkName,
                    StringComparison.OrdinalIgnoreCase));

            if (hasTestSdkReference)
            {
                return false;
            }

            bool publishExplicitlyEnabled = project.Project.HasPropertyWithValue("ArborPublishEnabled", "true");

            if (publishExplicitlyEnabled)
            {
                return true;
            }

            if (project.Project.HasPropertyWithValue("PackAsTool", "true", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (project.Project.PropertyGroups.Any(msBuildPropertyGroup =>
                    msBuildPropertyGroup.Properties.Any(msBuildProperty =>
                        msBuildProperty.Name.Equals("ArborPublishEnabled",
                            StringComparison.Ordinal) && msBuildProperty.Value == "false")))
            {
                return false;
            }

            return project.HasExplicitExeOutputType() || project.HasPublishPackageEnabled();
        }

        public bool HasPublishPackageEnabled() =>
            project.Project.HasPropertyWithValue("GeneratePackageOnBuild", "true", StringComparison.OrdinalIgnoreCase);

        public bool HasExplicitExeOutputType() =>
            project.Project.HasPropertyWithValue("OutputType", "Exe", StringComparison.OrdinalIgnoreCase);
    }
}