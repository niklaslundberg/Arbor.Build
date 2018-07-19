using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Arbor.X.Core.BuildVariables;

using Arbor.X.Core.Tools.Testing;
using Machine.Specifications;
using Serilog.Core;

namespace Arbor.X.Tests.Integration.VisualStudioVariableProvider
{
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    [Subject(typeof(Core.Tools.VisualStudio.VisualStudioVariableProvider))]
    public class when_getting_variables
    {
        static Core.Tools.VisualStudio.VisualStudioVariableProvider provider;

        static List<IVariable> enumerable;

        Establish context = () => { provider = new Core.Tools.VisualStudio.VisualStudioVariableProvider(); };

        Because of = () =>
        {
            var environmentVariable = new EnvironmentVariable(
                WellKnownVariables.ExternalTools_VisualStudio_Version_Allow_PreRelease,
                "true");

            enumerable = provider.GetEnvironmentVariablesAsync(
                    Logger.None,
                    new List<IVariable> { environmentVariable },
                    CancellationToken.None)
                .Result.ToList();
        };

        It should_return_a_list_of_visual_studio_versions = () => enumerable.ShouldNotBeNull();
    }
}
