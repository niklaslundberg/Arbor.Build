﻿using System;

namespace Arbor.Build.Core.Tools.MSBuild;

public sealed class TargetFramework : ValueObject<TargetFramework, string>
{
    public static readonly TargetFramework Net47 = new("net47");
    public static readonly TargetFramework Net48 = new("net48");
    public static readonly TargetFramework Net471 = new("net471");
    public static readonly TargetFramework Net472 = new("net472");
    public static readonly TargetFramework NetStandard2_0 = new("netstandard2.0");
    public static readonly TargetFramework NetStandard2_1 = new("netstandard2.1");
    public static readonly TargetFramework NetCoreApp3_1 = new("netcoreapp3.1");
    public static readonly TargetFramework NetCoreApp3_0 = new("netcoreapp3.0");
    public static readonly TargetFramework Net5_0 = new("net5.0");
    public static readonly TargetFramework Net6_0 = new("net6.0");
    public static readonly TargetFramework Net7_0 = new("net7.0");
    public static readonly TargetFramework Net8_0 = new("net8.0");
    public static readonly TargetFramework Net9_0 = new("net9.0");
    public static readonly TargetFramework Net10_0 = new("net10.0");
    public static readonly TargetFramework Net11_0 = new("net11.0");
    public static readonly TargetFramework Net12_0 = new("net12.0");
    public static readonly TargetFramework Empty = new("N/A");

    public override string ToString() => Value;

    public TargetFramework(string value) : base(value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        }
    }
}