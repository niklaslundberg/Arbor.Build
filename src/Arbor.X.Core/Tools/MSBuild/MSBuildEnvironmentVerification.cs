using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Tools.EnvironmentVariables;

using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.MSBuild
{
    [Priority(51)]
    [UsedImplicitly]
    public class MSBuildEnvironmentVerification : EnvironmentVerification
    {
        public MSBuildEnvironmentVerification()
        {
            RequiredValues.Add(WellKnownVariables.ExternalTools_MSBuild_ExePath);
        }
    }
}
