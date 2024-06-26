﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Arbor.Build.Core.Tools.MSBuild;
using Arbor.FS;
using Xunit;
using Xunit.Abstractions;
using Zio;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.MSBuildProjects;

public class FinWebApplicationProjectTypeId(ITestOutputHelper output)
{
    [Fact]
    public async Task ParseWebApplicationCsProjFile()
    {
        const string xml = """
                           <?xml version="1.0" encoding="utf-8"?>
                           <Project ToolsVersion="12.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                             <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
                             <PropertyGroup>
                               <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
                               <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
                               <ProductVersion>
                               </ProductVersion>
                               <SchemaVersion>2.0</SchemaVersion>
                               <ProjectGuid>{04854B5C-247C-4F59-834D-9ACF5048F29D}</ProjectGuid>
                               <ProjectTypeGuids>{349c5851-65df-11da-9384-00065b846f21};{fae04ec0-301f-11d3-bf4b-00c04f79efbc}</ProjectTypeGuids>
                               <OutputType>Library</OutputType>
                               <AppDesignerFolder>Properties</AppDesignerFolder>
                               <RootNamespace>Test</RootNamespace>
                               <AssemblyName>Test</AssemblyName>
                               <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
                               <UseIISExpress>true</UseIISExpress>
                               <IISExpressSSLPort>44432</IISExpressSSLPort>
                               <IISExpressAnonymousAuthentication />
                               <IISExpressWindowsAuthentication />
                               <IISExpressUseClassicPipelineMode>false</IISExpressUseClassicPipelineMode>
                               <TargetFrameworkProfile />
                               <UseGlobalApplicationHostFile />
                               <Use64BitIISExpress />
                               <NuGetPackageImportStamp>
                               </NuGetPackageImportStamp>
                             </PropertyGroup>
                           </Project>

                           """;

        FileEntry? tempFile = null;
        using var fs = new PhysicalFileSystem();
        try
        {

            tempFile = fs.GetFileEntry(Path.GetTempFileName().ParseAsPath());
            var stream = tempFile.Open(FileMode.Open, FileAccess.Write);
            await stream.WriteAllTextAsync(xml, Encoding.UTF8);

            var msBuildProject = await MsBuildProject.LoadFrom(tempFile);

            output.WriteLine(msBuildProject.ToString());

            Assert.Equal(2, msBuildProject.ProjectTypes.Length);
            Assert.Contains(ProjectType.Mvc5, msBuildProject.ProjectTypes);
            Assert.Contains(ProjectType.CSharp, msBuildProject.ProjectTypes);

            Assert.Equal(Guid.Parse("04854B5C-247C-4F59-834D-9ACF5048F29D"), msBuildProject.ProjectId);
        }
        finally
        {
            tempFile?.Delete();
        }
    }
}