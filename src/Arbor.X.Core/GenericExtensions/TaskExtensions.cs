using System.Threading.Tasks;

using JetBrains.Annotations;

namespace Arbor.X.Core.GenericExtensions
{
    public static class TaskExtensions
    {
        public static Task<T> AsCompletedTask<T>([CanBeNull] this T instance)
        {
            return Task.FromResult(instance);
        }
    }
}
