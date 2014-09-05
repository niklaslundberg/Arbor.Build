using System;
using System.Collections.Generic;
using System.IO;
using Arbor.Aesculus.Core;
using Arbor.X.Core.Logging;
using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;

namespace Arbor.X.Tests.Integration.Tests.MSpec
{
    [Subject(typeof(UnitTestFinder))]
    public class when_finding_mspec_assemblies_with_behaviors
    {
        static UnitTestFinder finder;
        static IReadOnlyCollection<string> dlls;

        Establish context = () =>
        {
            var logger = new ConsoleLogger { LogLevel = LogLevel.Verbose };
            finder = new UnitTestFinder(new List<Type>
                                        {
                                            typeof (BehaviorsAttribute)
                                        }, logger: logger);
        };

        Because of = () => { dlls = finder.GetUnitTestFixtureDlls(new DirectoryInfo(VcsPathHelper.FindVcsRootPath())); };

        It should_Behaviour = () =>
        {
            foreach (string dll in dlls)
            {
                Console.WriteLine(dll);
            }
        };
    }
}