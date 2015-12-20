using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arbor.X.Core.BuildVariables;
using Arbor.X.Core.Tools.EnvironmentVariables;

using JetBrains.Annotations;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(52)]
    [UsedImplicitly]
    public class NuGetEnvironmentVerification : EnvironmentVerification
    {
        public NuGetEnvironmentVerification()
        {
            RequiredValues.Add(WellKnownVariables.ExternalTools_NuGet_ExePath);
        }

        protected override bool PostVariableVerification(StringBuilder logger, IReadOnlyCollection<IVariable> buildVariables)
        {
            var variable = buildVariables.SingleOrDefault(item => item.Key == WellKnownVariables.ExternalTools_NuGet_ExePath);

            if (variable == null)
            {
                return false;
            }

            var nuGetExePath = variable.Value;

            var fileExists = File.Exists(nuGetExePath);

            if (!fileExists)
            {
                logger.AppendLine($"NuGet.exe path '{nuGetExePath}' does not exist");
            }

            return fileExists;
        }
    }
}
