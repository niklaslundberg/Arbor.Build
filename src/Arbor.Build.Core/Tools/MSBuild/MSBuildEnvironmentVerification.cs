using System.Collections.Generic;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.EnvironmentVariables;
using JetBrains.Annotations;

namespace Arbor.Build.Core.Tools.MSBuild;

[Priority(51)]
[UsedImplicitly]
public class MSBuildEnvironmentVerification : EnvironmentVerification
{
    protected override bool Enabled(IReadOnlyCollection<IVariable> buildVariables) => !buildVariables.GetBooleanByKey(WellKnownVariables.ExternalTools_MSBuild_DotNetEnabled);

    public MSBuildEnvironmentVerification() => RequiredValues.Add(WellKnownVariables.ExternalTools_MSBuild_ExePath);
}