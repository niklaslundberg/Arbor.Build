using Arbor.Build.Core.Tools.MSBuild;
using Xunit;

namespace Arbor.Build.Tests.Unit
{
    public class GitModelTests
    {
        [Fact]
        public void ParseGitFlowMasterShouldReturnInstance()
        {
            bool parsed = GitModel.TryParse("GitFlowBuildOnMaster", out var model);


            Assert.True(parsed);
            Assert.NotNull(model);
        }

        [Fact]
        public void ParseInvalidShouldReturnFalse()
        {
            bool parsed = GitModel.TryParse("BadValue", out var model);

            Assert.False(parsed);
            Assert.Null(model);
        }
    }
}