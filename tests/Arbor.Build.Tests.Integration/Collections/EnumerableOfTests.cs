using Arbor.Defensive.Collections;
using FluentAssertions;
using Xunit;

namespace Arbor.Build.Tests.Integration.Collections
{
    public class EnumerableOfTests
    {
        [Fact]
        public void EnumerableOfClassWith3PublicReadOnlyFieldsShouldList3Items()
        {
            EnumerableOf<EnumerableTestClass>.Items.Should().HaveCount(3);
        }

        private class EnumerableTestClass
        {
            public static readonly EnumerableTestClass A = new();
            public static readonly EnumerableTestClass B = new();
            public static readonly EnumerableTestClass C = new();
            public static readonly string D = "Other";
            public static readonly EnumerableTestClass? E = null;
        }
    }
}