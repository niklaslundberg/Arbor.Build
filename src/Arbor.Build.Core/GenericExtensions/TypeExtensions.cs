using System;
using System.Reflection;

namespace Arbor.Build.Core.GenericExtensions;

public static class TypeExtensions
{
    extension(Type type)
    {
        public bool HasSingleDefaultConstructor()
        {
            ArgumentNullException.ThrowIfNull(type);

            return type.GetConstructors().Length == 1 &&
                   type.GetConstructor([])?.GetParameters().Length == 0;
        }

        public bool IsConcretePublicClassImplementing<T>()
        {
            ArgumentNullException.ThrowIfNull(type);

            bool isConcretePublicClassImplementing = type is { IsClass: true, IsPublic: true } && typeof(T).IsAssignableFrom(type);

            return isConcretePublicClassImplementing;
        }
    }

    extension(FieldInfo fieldInfo)
    {
        public bool IsPublicConstant()
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldInfo));
            }

            bool isPublicConstant = fieldInfo is { IsLiteral: true, IsPublic: true };

            return isPublicConstant;
        }

        public bool IsPublicStatic()
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldInfo));
            }

            bool isPublicConstant = fieldInfo is { IsStatic: true, IsPublic: true };

            return isPublicConstant;
        }

        public bool IsPublicConstantOrStatic()
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