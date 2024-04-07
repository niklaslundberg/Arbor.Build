using System;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Logging;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Arbor.Build.Tests.Unit;

public class InitializeLevelSwitch
{
    [Fact]
    public void WhenEmptyArgumentListItShouldInitializeWithInformationLevel()
    {
        var environmentVariables = new EnvironmentVariables();
        environmentVariables.SetEnvironmentVariable(WellKnownVariables.LogLevel, "");

        LoggingLevelSwitch loggingLevelSwitch = LogLevelHelper.GetLevelSwitch([], environmentVariables);

        Assert.NotNull(loggingLevelSwitch);
        Assert.Equal(LogEventLevel.Information, loggingLevelSwitch.MinimumLevel);
    }

    [Fact]
    public void WhenNullArgumentListItShouldInitializeWithInformationLevel()
    {
        var environmentVariables = new EnvironmentVariables();
        environmentVariables.SetEnvironmentVariable(WellKnownVariables.LogLevel, "");
        LoggingLevelSwitch loggingLevelSwitch = LogLevelHelper.GetLevelSwitch(null, environmentVariables);

        Assert.NotNull(loggingLevelSwitch);
        Assert.Equal(LogEventLevel.Information, loggingLevelSwitch.MinimumLevel);
    }

    [Fact]
    public void WhenUsingInvvalidLogLevelArgumentItShouldInitializeWithSuppliedLevel()
    {
        string[] args = [$"{WellKnownVariables.LogLevel}="];
        LoggingLevelSwitch loggingLevelSwitch = LogLevelHelper.GetLevelSwitch(args, EnvironmentVariables.Empty);

        Assert.NotNull(loggingLevelSwitch);
        Assert.Equal(LogEventLevel.Information, loggingLevelSwitch.MinimumLevel);
    }

    [Fact]
    public void WhenUsingLogLevelArgumentItShouldInitializeWithSuppliedLevel()
    {
        string[] args = [$"{WellKnownVariables.LogLevel}=Debug"];
        LoggingLevelSwitch loggingLevelSwitch = LogLevelHelper.GetLevelSwitch(args, EnvironmentVariables.Empty);

        Assert.NotNull(loggingLevelSwitch);
        Assert.Equal(LogEventLevel.Debug, loggingLevelSwitch.MinimumLevel);
    }

}