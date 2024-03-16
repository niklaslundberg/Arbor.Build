using System;
using System.Collections.Generic;
using Arbor.Build.Core;
using Arbor.Build.Core.BuildVariables;
using Serilog.Core;
using Xunit;
using Xunit.Abstractions;

namespace Arbor.Build.Tests.Integration.WellknownVariables;

public class VariableCompatibilityTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void ArborXShouldBeTranslatedToArborBuild()
    {
        var variables = new List<IVariable> { new BuildVariable("Arbor.X.Test", "testvalue") };

        variables.AddCompatibilityVariables(Logger.None);

        foreach (var variable in variables)
        {
            testOutputHelper.WriteLine(variable.Key + ": " + variable.Value);
        }

        Assert.Equal(4, variables.Count);
        Assert.Contains(variables, variable => variable.Key.Equals("Arbor.X.Test", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(variables, variable => variable.Key.Equals("Arbor.Build.Test", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(variables, variable => variable.Key.Equals("Arbor_X_Test", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(variables, variable => variable.Key.Equals("Arbor_Build_Test", StringComparison.OrdinalIgnoreCase));
    }
}