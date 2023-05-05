using System;
using System.Linq;

namespace Arbor.Build.Core.Tools.MSBuild;

public static class SolutionProjectExtensions
{
    public static bool PublishEnabled(this SolutionProject project)
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

        if (project.Project.HasPropertyWithValue("PackAsTool", "true", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool publishExplicitlyEnabled = project.Project.HasPropertyWithValue("ArborPublishEnabled", "true");

        if (publishExplicitlyEnabled)
        {
            return true;
        }

        if (project.Project.PropertyGroups.Any(msBuildPropertyGroup =>
                msBuildPropertyGroup.Properties.Any(msBuildProperty =>
                    msBuildProperty.Name.Equals("ArborPublishEnabled",
                        StringComparison.Ordinal) && msBuildProperty.Value == "false")))
        {
            return false;
        }

        return HasExplicitExeOutputType(project) || HasPublishPackageEnabled(project);
    }

    public
        static bool HasPublishPackageEnabled(this SolutionProject project) =>
        project.Project.HasPropertyWithValue("GeneratePackageOnBuild", "true");

    public
        static bool HasExplicitExeOutputType(this SolutionProject project) =>
        project.Project.HasPropertyWithValue("OutputType", "Exe");
}