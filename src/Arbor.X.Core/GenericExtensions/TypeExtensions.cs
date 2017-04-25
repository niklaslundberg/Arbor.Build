using System;
using System.Reflection;
using JetBrains.Annotations;

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

            bool isConcretePublicClassImplementing = type.IsClass && type.IsPublic && typeof(T).IsAssignableFrom(type);

            return isConcretePublicClassImplementing;
        }

        public static bool IsPublicConstant([NotNull] this FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldInfo));
            }

            bool isPublicConstant = fieldInfo.IsLiteral && fieldInfo.IsPublic;

            return isPublicConstant;
        }

        public static bool IsPublicStatic([NotNull] this FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldInfo));
            }

            bool isPublicConstant = fieldInfo.IsStatic && fieldInfo.IsPublic;

            return isPublicConstant;
        }

        public static bool IsPublicConstantOrStatic([NotNull] this FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldInfo));
            }

            bool isPublicConstant = fieldInfo.IsPublicStatic() || fieldInfo.IsPublicConstant();

            return isPublicConstant;
        }
    }
}
