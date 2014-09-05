using System.Collections.Generic;
using System.IO;
using System.Threading;
using Arbor.Aesculus.Core;
using Arbor.X.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.IO;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;
using Machine.Specifications.Model;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Subject(typeof (Subject))]
    public class when_running_mspec_on_self
    {
        Establish context = () =>
        {
            var root = Path.Combine(VcsPathHelper.FindVcsRootPath(), "src");

            var combine = Path.Combine(root, "Arbor.X.Tests.Integration");

           var tempPath = Path.Combine(Path.GetTempPath(), "Arbor.X", "MSpec");

            var tempDirectory = new DirectoryInfo(tempPath).EnsureExists();
            
            DirectoryCopy.Copy(combine, tempDirectory.FullName);

            testRunner = new MSpecTestRunner();
            variables.Add(new EnvironmentVariable(WellKnownVariables.ExternalTools, Path.Combine(VcsPathHelper.FindVcsRootPath(), "tools", "external")));

            variables.Add(new EnvironmentVariable(WellKnownVariables.SourceRootOverride, tempDirectory.FullName));
        };

        Because of = () => ExitCode = testRunner.ExecuteAsync(new ConsoleLogger() {LogLevel = LogLevel.Verbose}, variables, new CancellationToken()).Result;

        It should_Behaviour = () => ExitCode.IsSuccess.ShouldBeTrue();
        static MSpecTestRunner testRunner;
        static List<IVariable> variables = new List<IVariable>();
        static ExitCode ExitCode;
    }
}