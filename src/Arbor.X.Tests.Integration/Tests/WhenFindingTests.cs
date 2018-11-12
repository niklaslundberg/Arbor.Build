using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Arbor.Build.Core.Tools.Testing;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Arbor.Build.Tests.Integration.Tests
{
    public class WhenFindingTests
    {
        [Fact]
        public void ItShouldFindAssemblies()
        {
            //Logger logger = new LoggerConfiguration().WriteTo.Debug(LogEventLevel.Verbose).CreateLogger();

            //var unitTestFinder = new UnitTestFinder(new[] { typeof(FactAttribute) }, true, logger);

            //HashSet<string> unitTestFixtureDlls = unitTestFinder.GetUnitTestFixtureDlls(new DirectoryInfo(@"C:\Temp\arbor.x\D\131791410344759485"), false, new[]{"Arbor.Build.Tests"}.ToImmutableArray());

            //Assert.NotEmpty(unitTestFixtureDlls);
        }
    }
}
