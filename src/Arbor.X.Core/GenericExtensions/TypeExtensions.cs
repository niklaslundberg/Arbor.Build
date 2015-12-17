using System;

namespace Arbor.X.Core.GenericExtensions
{
    public static class TypeExtensions
    {
        public static bool IsConcretePublicClassImplementing<T>(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var isConcretePublicClassImplementing = type.IsClass && type.IsPublic && typeof(T).IsAssignableFrom(type);

            return isConcretePublicClassImplementing;
        }
    }
}
