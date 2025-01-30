using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arbor.Build.Core.GenericExtensions;
using Machine.Specifications;
using Shouldly;
using Xunit;

namespace Arbor.Build.Tests.Integration.Collections;

public class EnumerableExtensionsTests
{
    [Fact]
    public void SafeToReadOnlyCollectionShouldHaveSameCount() =>
        new List<int> {1, 2, 3}.SafeToReadOnlyCollection().Length.ShouldEqual(3);

    [Fact]
    public void SafeToReadOnlyCollectionForNullShouldReturnEmptyCollection() =>
        ShouldExtensionMethods.ShouldBeEmpty(((IEnumerable<int>?)null).SafeToReadOnlyCollection());

    [Fact]
    public void ToReadOnlyCollection() => new List<int> { 1, 2, 3 }.ToReadOnlyCollection().Count.ShouldEqual(3);

    [Fact]
    public void ImmutableArrayToReadOnlyCollectionShouldReturnEquivalentInstance()
    {
        var immutableArray = new List<int> {1, 2, 3}.ToImmutableArray();

        immutableArray.ToReadOnlyCollection().ShouldContainOnly(immutableArray);
    }

    [Fact]
    public void DefaultImmutableArrayToReadOnlyCollectionShouldReturnEmptyCollection()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        ImmutableArray<string> immutableArray = default;

        ShouldExtensionMethods.ShouldBeEmpty(immutableArray.ToReadOnlyCollection());
    }

    [Fact]
    public void ToReadOnlyCollectionForNullShouldThrow()
    {
        List<string>? list = null;

        void ReadOnlyCollection() => list!.ToReadOnlyCollection();

        Should.Throw<ArgumentNullException>(ReadOnlyCollection);
    }

    [Fact]
    public void NotNullShouldFilterOutNulls() =>
        new List<string?> {"", null, "123", "abc"}.NotNull().ToList().Count.ShouldEqual(3);

    [Fact]
    public void ValueTupleNotNullShouldFilterOutNulls() =>
        new List<(string?, string?)> { ("", null), ("123", "abc"), (null, "abc") }.NotNull().ShouldHaveSingleItem();

    [Fact]
    public void ValueTuple3NotNullShouldFilterOutNulls() =>
        new List<(string?, string?, string?)>
        {
            ("", null, ""), ("123", "abc", ""), (null, "abc", ""), ("null", "abc", null)
        }.NotNull().ShouldHaveSingleItem();
}