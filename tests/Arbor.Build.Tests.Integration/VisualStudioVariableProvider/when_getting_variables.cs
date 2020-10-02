using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Arbor.Build.Core.BuildVariables;
using Arbor.Build.Core.Tools.Testing;
using Machine.Specifications;
using Serilog.Core;
using Zio.FileSystems;

namespace Arbor.Build.Tests.Integration.VisualStudioVariableProvider
{
    [Tags(MSpecInternalConstants.RecursiveArborXTest)]
    [Subject(typeof(Core.Tools.VisualStudio.VisualStudioVariableProvider))]
    public class when_getting_variables
    {
        static Core.Tools.VisualStudio.VisualStudioVariableProvider provider;

        static List<IVariable> enumerable;

        Establish context = () => provider = new Core.Tools.VisualStudio.VisualStudioVariableProvider(new PhysicalFileSystem());

        Because of = () =>
        {
            var environmentVariable = new BuildVariable(
                WellKnownVariables.ExternalTools_VisualStudio_Version_Allow_PreRelease,
                "true");

            enumerable = provider.GetBuildVariablesAsync(
                    Logger.None,
                    new List<IVariable> { environmentVariable },
                    CancellationToken.None)
                .Result.ToList();
        };

        It should_return_a_list_of_visual_studio_versions = () => enumerable.ShouldNotBeNull();
    }
}
