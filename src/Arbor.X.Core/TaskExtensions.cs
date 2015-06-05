using System.Threading.Tasks;

namespace Arbor.X.Core
{
    public static class TaskExtensions
    {
        public static Task<T> AsCompletedTask<T>(this T instance)
        {
            return Task.FromResult(instance);
        }
    }
}