using System;
using System.Reflection;

namespace Arbor.Build.Core.GenericExtensions;

public static class TypeExtensions
{
    public static bool HasSingleDefaultConstructor(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.GetConstructors().Length == 1 &&
               type.GetConstructor([])?.GetParameters().Length == 0;
    }

    public static bool IsConcretePublicClassImplementing<T>(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        bool isConcretePublicClassImplementing = type is { IsClass: true, IsPublic: true } && typeof(T).IsAssignableFrom(type);

        return isConcretePublicClassImplementing;
    }

    public static bool IsPublicConstant(this FieldInfo fieldInfo)
    {
        if (fieldInfo == null)
        {
            throw new ArgumentNullException(nameof(fieldInfo));
        }

        bool isPublicConstant = fieldInfo is { IsLiteral: true, IsPublic: true };

        return isPublicConstant;
    }

    public static bool IsPublicStatic(this FieldInfo fieldInfo)
    {
        if (fieldInfo == null)
        {
            throw new ArgumentNullException(nameof(fieldInfo));
        }

        bool isPublicConstant = fieldInfo is { IsStatic: true, IsPublic: true };

        return isPublicConstant;
    }

    public static bool IsPublicConstantOrStatic(this FieldInfo fieldInfo)
    {
        if (fieldInfo == null)
        {
            throw new ArgumentNullException(nameof(fieldInfo));
        }

        bool isPublicConstant = fieldInfo.IsPublicStatic() || fieldInfo.IsPublicConstant();

        return isPublicConstant;
    }
}