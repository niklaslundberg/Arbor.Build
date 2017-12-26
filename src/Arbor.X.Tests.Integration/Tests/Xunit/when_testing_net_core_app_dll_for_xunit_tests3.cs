using System.Collections.Generic;
using Arbor.Processing.Core;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.Xunit
{
    [Ignore("local")]
    [Subject(typeof(UnitTestFinder))]
    public class when_testing_net_core_app_dll_for_xunit_tests3
    {
        static UnitTestFinder finder;
        static bool isTestType;

        Establish context = () =>
        {
            consoleLogger = new ConsoleLogger { LogLevel = LogLevel.Verbose };
            variables = new List<IVariable>
            {
                new EnvironmentVariable(WellKnownVariables.SourceRoot, @"C:\projects\niklas\mohedascoutkar"),
                new EnvironmentVariable(WellKnownVariables.XUnitNetCoreAppEnabled, "true"),
                new EnvironmentVariable(WellKnownVariables.XUnitNetCoreAppDllPath, @"C:\Tools\Xunit\netcoreapp2.0\xunit.console.dll"),
                new EnvironmentVariable(WellKnownVariables.TestsAssemblyStartsWith, @"moheda"),
                new EnvironmentVariable(WellKnownVariables.Artifacts, @"C:\Work\Niklas\artifacts"),
                new EnvironmentVariable(WellKnownVariables.ReportPath, @"C:\Work\Niklas\artifacts\reports"),
                new EnvironmentVariable(WellKnownVariables.RunTestsInReleaseConfigurationEnabled, "false"),
                new EnvironmentVariable(WellKnownVariables.DotNetExePath, @"C:\program files\dotnet\dotnet.exe"),
            };
        };

        Because of =
            () =>
            {
                string currentDirectory = @"C:\Projects\Niklas\mohedascoutkar\";

                XunitNetCoreAppTestRunner runner = new XunitNetCoreAppTestRunner();

                exitCode =  runner.ExecuteAsync(consoleLogger, variables, default).Result;
            };

        It should_Behaviour = () => exitCode.IsSuccess.ShouldBeTrue();
        static HashSet<string> unitTestFixtureDlls;
        static ConsoleLogger consoleLogger;
        static List<IVariable> variables;
        static ExitCode exitCode;
    }
}