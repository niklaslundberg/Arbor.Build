// ReSharper disable InconsistentNaming

namespace Arbor.Build.Core.BuildVariables;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.NamingRules",
    "SA1310:Field names must not contain underscore",
    Justification = "Variables")]
public partial class WellKnownVariables
{
    [VariableDescription("Jenkins HOME path")]
    public const string ExternalTools_Jenkins_JenkinsHome =
        "JENKINS_HOME";
}