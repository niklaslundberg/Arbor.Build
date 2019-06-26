// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly:
    System.Diagnostics.CodeAnalysis.SuppressMessage("Potential Code Quality Issues",
        "RECS0108:Warns about static fields in generic types",
        Justification = "Optimization",
        Scope = "member",
        Target = "~F:Arbor.Defensive.Maybe`1.empty")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Optimization", Scope = "member", Target = "~M:Arbor.Defensive.Maybe`1.Empty~Arbor.Defensive.Maybe`1")]

