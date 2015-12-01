using System;
using Etg.Yams.Application;
using Etg.Yams.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Utils
{
    [TestClass]
    public class DeploymentUtilsTest
    {
        [TestMethod]
        public void TestGetDeploymentRelativePath()
        {
            AppIdentity appIdentity = new AppIdentity("id", new Version("1.0.0"));
            Assert.AreEqual("id\\1.0.0", DeploymentUtils.GetDeploymentRelativePath(appIdentity));
        }
    }
}
