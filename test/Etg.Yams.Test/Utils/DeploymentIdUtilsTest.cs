using Etg.Yams.Utils;
using Xunit;

namespace Etg.Yams.Test.Utils
{
    public class DeploymentIdUtilsTest
    {
        [Fact]
        public void TestGetCloudServiceDeploymentId()
        {
            Assert.Equal("testdeploymentid", DeploymentIdUtils.CloudServiceDeploymentId);
        }
    }
}
