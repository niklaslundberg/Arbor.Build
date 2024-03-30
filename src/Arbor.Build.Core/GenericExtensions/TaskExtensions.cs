using System.Threading.Tasks;

namespace Arbor.Build.Core.GenericExtensions;

public static class TaskExtensions
{
    public static Task<T?> AsCompletedTask<T>(this T? instance) => Task.FromResult(instance);
}