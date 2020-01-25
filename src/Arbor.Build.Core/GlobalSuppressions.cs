// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage(
        "StyleCop.CSharp.NamingRules",
        "SA1310:Field names must not contain underscore",
        Justification = "Variables",
        Scope = "type",
        Target = "Arbor.Build.Core.BuildVariables.WellKnownVariables")]

[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules",
        "SA1401:Fields must be private",
        Justification = "LegacyRule",
        Scope = "member",
        Target = "~F:Arbor.Build.Core.Tools.EnvironmentVariables.EnvironmentVerification.RequiredValues")]
