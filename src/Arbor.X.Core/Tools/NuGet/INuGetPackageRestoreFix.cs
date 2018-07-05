using System.Threading.Tasks;
using Serilog;

namespace Arbor.X.Core.Tools.NuGet
{
    public interface INuGetPackageRestoreFix
    {
        Task FixAsync(string packagesDirectory, ILogger logger);
    }
}
