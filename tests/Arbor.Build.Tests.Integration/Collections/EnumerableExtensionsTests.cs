using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Arbor.Defensive.Collections;
using FluentAssertions;
using Xunit;

namespace Arbor.Build.Tests.Integration.Collections
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void SafeToReadOnlyCollectionShouldHaveSameCount() =>
            new List<int> {1, 2, 3}.SafeToReadOnlyCollection().Should().HaveCount(3);

        [Fact]
        public void SafeToReadOnlyCollectionForNullShouldReturnEmptyCollection() =>
            ((IEnumerable<int>?)null).SafeToReadOnlyCollection().Should().BeEmpty();

        [Fact]
        public void ToReadOnlyCollection() => new List<int> {1, 2, 3}.ToReadOnlyCollection().Should().HaveCount(3);

        [Fact]
        public void ImmutableArrayToReadOnlyCollectionShouldReturnEquivalentInstance()
        {
            var immutableArray = new List<int> {1, 2, 3}.ToImmutableArray();

            immutableArray.ToReadOnlyCollection().Should().BeEquivalentTo(immutableArray);
        }

        [Fact]
        public void DefaultImmutableArrayToReadOnlyCollectionShouldReturnEmptyCollection()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            ImmutableArray<string> immutableArray;

            immutableArray.ToReadOnlyCollection().Should().BeEmpty();
        }

        [Fact]
        public void ToReadOnlyCollectionForNullShouldThrow()
        {
            List<string>? list = null;

            Action toReadOnlyCollection = () => list!.ToReadOnlyCollection();

            toReadOnlyCollection.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ValueToImmutableArrayShouldHave1Element() => 1.ValueToImmutableArray().Should().HaveCount(1);

        [Fact]
        public void NotNullShouldFilterOutNulls() =>
            new List<string?> {"", null, "123", "abc"}.NotNull().Should().HaveCount(3);

        [Fact]
        public void ValueTupleNotNullShouldFilterOutNulls() =>
            new List<(string?, string?)> {("", null), ("123", "abc"), (null, "abc")}.NotNull().Should().HaveCount(1);

        [Fact]
        public void ValueTuple3NotNullShouldFilterOutNulls() =>
            new List<(string?, string?, string?)>
            {
                ("", null, ""), ("123", "abc", ""), (null, "abc", ""), ("null", "abc", null)
            }.NotNull().Should().HaveCount(1);
    }
}