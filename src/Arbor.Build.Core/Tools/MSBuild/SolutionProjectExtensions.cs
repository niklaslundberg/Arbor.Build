using System;
using System.Linq;

namespace Arbor.Build.Core.Tools.MSBuild
{
    public static class SolutionProjectExtensions
    {
        public static bool PublishEnabled(this SolutionProject project)
        {
            if (project.Project.Sdk == DotNetSdk.DotnetWeb)
            {
                return true;
            }

            if (project.NetFrameworkGeneration == NetFrameworkGeneration.NetFramework)
            {
                return false;
            }

            bool hasTestSdkReference = project.Project.PackageReferences.Any(reference => reference.Package is {} packageName &&
                                                                              packageName.Equals(DotNetSdk.Test.SdkName,
                                                                                  StringComparison.OrdinalIgnoreCase));
            if (hasTestSdkReference)
            {
                return false;
            }

            bool hasArborPublishOrDefault = project.Project.HasPropertyWithValue("ArborPublishEnabled", "true") ||
                                            !project.Project.PropertyGroups.Any(msBuildPropertyGroup =>
                                                msBuildPropertyGroup.Properties.Any(msBuildProperty =>
                                                    msBuildProperty.Name.Equals("ArborPublishEnabled",
                                                        StringComparison.Ordinal)));
            return
                hasArborPublishOrDefault ||
                HasExplicitExeOutputType(project) ||
                HasPublishPackageEnabled(project);
        }

        public
            static bool HasPublishPackageEnabled(this SolutionProject project) =>
            project.Project.HasPropertyWithValue("GeneratePackageOnBuild", "true");

        public
            static bool HasExplicitExeOutputType(this SolutionProject project) =>
            project.Project.HasPropertyWithValue("OutputType", "Exe");
    }
}