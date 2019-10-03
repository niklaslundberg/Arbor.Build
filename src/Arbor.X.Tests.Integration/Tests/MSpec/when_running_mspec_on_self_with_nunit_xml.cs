using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.IO;
using Arbor.Build.Core.Tools.Testing;
using Arbor.Processing;
using Machine.Specifications;
using Serilog.Core;

namespace Arbor.Build.Tests.Integration.Tests.MSpec
{
    [Ignore("Self")]
    [Subject(typeof(MSpecTestRunner))]
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    public class when_running_mspec_on_self_with_nunit_xml
    {
        static MSpecTestRunner testRunner;
        static readonly List<IVariable> variables = new List<IVariable>();
        static ExitCode ExitCode;
        static string mspecReports;
        static ExitCode exitCode;

        static DirectoryInfo tempDirectory;

        Cleanup after = () =>
        {
            Thread.Sleep(1000);

            try
            {
                tempDirectory.DeleteIfExists();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not cleanup test directory, " + ex);
            }
        };

        Establish context = () =>
        {
            string root = Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "src");

            string combine = Path.Combine(root, "Arbor.X.Tests.Integration", "bin", "debug");

            string tempPath = Path.Combine(Path.GetTempPath(),
                $"{DefaultPaths.TempPathPrefix}_mspec_self_rep_{DateTime.Now.ToString("yyyyMMddHHmmssfff_", CultureInfo.InvariantCulture)}{Guid.NewGuid().ToString().Substring(0, 8)}");

            tempDirectory = new DirectoryInfo(tempPath).EnsureExists();

            DirectoryInfo binDirectory = tempDirectory.CreateSubdirectory("bin");

            using (File.Create(Path.Combine(tempDirectory.FullName, ".gitattributes.")))
            {
            }

            exitCode =
                DirectoryCopy.CopyAsync(combine,
                        binDirectory.FullName,
                        pathLookupSpecificationOption: new PathLookupSpecification())
                    .Result;

            testRunner = new MSpecTestRunner();
            variables.Add(new BuildVariable(WellKnownVariables.ExternalTools,
                Path.Combine(VcsTestPathHelper.FindVcsRootPath(), "tools", "external")));

            variables.Add(new BuildVariable(WellKnownVariables.SourceRootOverride, tempDirectory.FullName));
            variables.Add(new BuildVariable(WellKnownVariables.SourceRoot, tempDirectory.FullName));
            variables.Add(new BuildVariable(WellKnownVariables.MSpecJUnitXslTransformationEnabled, "true"));
            variables.Add(new BuildVariable(WellKnownVariables.RunTestsInReleaseConfigurationEnabled, "false"));

            mspecReports = Path.Combine(tempDirectory.FullName, "MSpecReports");

            new DirectoryInfo(mspecReports).EnsureExists();

            variables.Add(new BuildVariable(WellKnownVariables.ExternalTools_MSpec_ReportPath, mspecReports));
        };

        Because of =
            () =>
                ExitCode =
                    testRunner.ExecuteAsync(Logger.None,
                            variables,
                            new CancellationToken())
                        .Result;

        It shoud_have_created_html_report = () =>
        {
            var reports = new DirectoryInfo(mspecReports);
            DirectoryInfo htmlDirectory = reports.GetDirectories()
                .SingleOrDefault(dir => dir.Name.Equals("html", StringComparison.OrdinalIgnoreCase));

            FileInfo[] files = reports.GetFiles("*.html", SearchOption.AllDirectories);

            foreach (FileInfo fileInfo in files)
            {
                Console.WriteLine(fileInfo.FullName);
            }

            htmlDirectory.ShouldNotBeNull();
        };

        It shoud_have_created_xml_report = () =>
        {
            var reports = new DirectoryInfo(mspecReports);

            FileInfo[] files = reports.GetFiles("*.xml", SearchOption.AllDirectories);

            foreach (FileInfo fileInfo in files)
            {
                Console.WriteLine(fileInfo.FullName);
            }

            files.Length.ShouldNotEqual(0);
        };

        It should_return_success = () => ExitCode.IsSuccess.ShouldBeTrue();
    }
}
