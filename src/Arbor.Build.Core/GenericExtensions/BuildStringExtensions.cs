using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.Build.Core.GenericExtensions;

public static class BuildStringExtensions
{
    public static bool StartsWithAny(
        this string value,
        IReadOnlyCollection<string> whatToFind,
        StringComparison stringComparison)
    {
        ArgumentNullException.ThrowIfNull(value);

        ArgumentNullException.ThrowIfNull(whatToFind);

        return whatToFind.Any(current => value.StartsWith(current, stringComparison));
    }

    public static bool TryParseString(this string value, out string result, string defaultValue = "")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = defaultValue;
            return false;
        }

        result = value;
        return true;
    }

    public static string LeftPad(this string value, int totaltLenght, char character)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new string(character, totaltLenght);
        }

        if (totaltLenght <= 0)
        {
            return value;
        }

        int lengthDiff = totaltLenght - value.Length;

        if (lengthDiff <= 0)
        {
            return value;
        }

        return value.PadLeft(totaltLenght, character);
    }

    public static string Wrap(this string text, string wrap) => wrap + text + wrap;

    public static IEnumerable<string> WrapItems(this IEnumerable<string> enumerable, string wrap) => enumerable.Select(item => item.Wrap(wrap));

    public static string? WithDefault(this string? text, string? defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return defaultValue;
        }

        return text;
    }
}