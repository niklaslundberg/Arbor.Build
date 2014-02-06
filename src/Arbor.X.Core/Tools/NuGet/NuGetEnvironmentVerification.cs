using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arbor.X.Core.BuildVariables;

namespace Arbor.X.Core.Tools.NuGet
{
    [Priority(52)]
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
                logger.AppendLine(string.Format("NuGet.exe path '{0}' does not exist", nuGetExePath));
            }

            return fileExists;
        }
    }
}