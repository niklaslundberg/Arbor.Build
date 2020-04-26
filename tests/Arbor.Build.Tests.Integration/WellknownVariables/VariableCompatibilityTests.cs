using System.Collections.Generic;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;

namespace Arbor.Build.Tests.Integration.WellknownVariables
{
    public class VariableCompatibilityTests
    {
        public VariableCompatibilityTests(ITestOutputHelper testOutputHelper) => output = testOutputHelper;

        readonly ITestOutputHelper output;

        [Fact]
        public void ArborXShouldBeTranslatedToArborBuild()
        {
            var variables = new List<IVariable> { new BuildVariable("Arbor.X.Test", "testvalue") };

            variables.AddCompatibilityVariables(Logger.None);

            foreach (var variable in variables)
            {
                output.WriteLine(variable.Key + ": " + variable.Value);
            }

            Assert.Equal(4, variables.Count);
            Assert.Contains(variables, variable => variable.Key.Equals("Arbor.X.Test"));
            Assert.Contains(variables, variable => variable.Key.Equals("Arbor.Build.Test"));
            Assert.Contains(variables, variable => variable.Key.Equals("Arbor_X_Test"));
            Assert.Contains(variables, variable => variable.Key.Equals("Arbor_Build_Test"));
        }
    }
}