using System.Collections.Generic;
using Arbor.Build.Core.BuildVariables;
using FluentAssertions;
using Xunit;

namespace Arbor.Build.Tests.Unit.Variables;

public class VariableTests
{
    [Fact(DisplayName = "Given that both the key and value are defined, ToString() should include key and value")]
    public void ToStringNotNull()
    {
        IVariable variable = new BuildVariable("A", "123");

        variable.ToString().Should().Be("A: '123'");
    }

    [Theory(DisplayName = "Given that only the key and value are defined, ToString() should only include key")]
    [InlineData("")]
    [InlineData(null)]
    public void ToStringValueNull(string? value)
    {
        IVariable variable = new BuildVariable("A", value);

        variable.ToString().Should().Be("A: <empty>");
    }

    public static IEnumerable<object[]> Secrets() =>
    [
        ["password"],
        ["apikey"],
        ["username"],
        ["pw"],
        ["token"],
        ["jwt"],
        ["connectionString"],
        ["client_secret"]
    ];

    [Theory]
    [MemberData(nameof(Secrets))]
    public void ToStringForSecret(string key)
    {
        var buildVariable = new BuildVariable(key, null);

        buildVariable.ToString().Should().Be($"{key}: *****");
    }

    [Theory]
    [MemberData(nameof(Secrets))]
    public void ToStringForSecretUpper(string key)
    {
        string keyUpper = key.ToUpperInvariant();

        var buildVariable = new BuildVariable(keyUpper, null);

        buildVariable.ToString().Should().Be($"{keyUpper}: *****");
    }
}