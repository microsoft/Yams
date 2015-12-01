using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Utils
{
    [TestClass]
    public class DeploymentIdUtilsTest
    {
        [TestMethod]
        public void TestGetCloudServiceDeploymentId()
        {
            Assert.AreEqual("testdeploymentid", DeploymentIdUtils.CloudServiceDeploymentId);
        }
    }
}
