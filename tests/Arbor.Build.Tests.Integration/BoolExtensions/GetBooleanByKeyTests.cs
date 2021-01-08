using Arbor.Build.Core.BuildVariables;
using Xunit;

namespace Arbor.Build.Tests.Integration.BoolExtensions
{
    public class GetBooleanByKeyTests
    {
        [Fact]
        public void InvalidValueWithNoExplicitDefaultShouldBeFalse()
        {
            IVariable[] variables = { new BuildVariable("abc", "123") };

            bool value = variables.GetBooleanByKey("abc");

            Assert.False(value);
        }

        [Fact]
        public void InvalidWithDefaultTrueShouldBeTrue()
        {
            IVariable[] variables = { new BuildVariable("abc", "123") };

            bool value = variables.GetBooleanByKey("abc", true);

            Assert.True(value);
        }

        [Fact]
        public void ValueTrueShouldBeTrue()
        {
            IVariable[] variables = { new BuildVariable("abc", "true") };

            bool value = variables.GetBooleanByKey("abc");

            Assert.True(value);
        }
    }
}
