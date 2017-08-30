using System.Threading.Tasks;
using Arbor.X.Core.Logging;

namespace Arbor.X.Core.Tools.NuGet
{
    public interface INuGetPackageRestoreFix
    {
        Task FixAsync(string packagesDirectory, ILogger logger);
    }
}
