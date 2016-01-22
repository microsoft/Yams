using System;
using System.IO;
using System.Linq;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;
using Etg.Yams.Tools.Test.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Etg.Yams.Test.Storage
{
    [TestClass]
    public class DeploymentConfigTest
    {
        private readonly string _deploymentConfigFilePath = Path.Combine("Data", "DeploymentConfig",
            "DeploymentConfig.json");

        private DeploymentConfig _deploymentConfig;

        [TestInitialize]
        public void TestInitialize()
        {
            string json = ReadDeploymentConfig();
            _deploymentConfig = new DeploymentConfig(json);
        }

        private string ReadDeploymentConfig()
        {
            return File.ReadAllText(_deploymentConfigFilePath);
        }

        [TestMethod]
        public void TestListApplications()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"app1", "app2", "app3"},
                _deploymentConfig.ListApplications());
        }

        [TestMethod]
        public void TestListApplicationsForGivenDeploymentId()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"app1", "app2"},
                _deploymentConfig.ListApplications("deploymentid1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"app1"},
                _deploymentConfig.ListApplications("deploymentid2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"app3"},
                _deploymentConfig.ListApplications("deploymentid3"));
            AssertUtils.ContainsSameElementsInAnyOrder(new string[] {},
                _deploymentConfig.ListApplications("deploymentid4"));
        }

        [TestMethod]
        public void TestListVersions()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"1.0.0", "1.0.1"}, _deploymentConfig.ListVersions("app1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"2.0.0"}, _deploymentConfig.ListVersions("app3"));
        }

        [TestMethod]
        public void TestListVersionsForAnAppThatIsNotThere()
        {
            Assert.IsFalse(_deploymentConfig.ListVersions("UnknownApp").Any());
        }

        [TestMethod]
        public void TestListVersionsWithDeploymentId()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"1.0.0", "1.0.1"},
                _deploymentConfig.ListVersions("app1", "deploymentid1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"1.0.1"},
                _deploymentConfig.ListVersions("app1", "deploymentid2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new string[] {},
                _deploymentConfig.ListVersions("app1", "deploymentid13"));
        }

        [TestMethod]
        public void TestListVersionsWithDeploymentIdForAnAppThatIsNotThere()
        {
            Assert.IsFalse(_deploymentConfig.ListVersions("UnknownApp", "deploymentid1").Any());
        }

        [TestMethod]
        public void TestListDeploymentsForApp()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid1", "deploymentid2"},
                _deploymentConfig.ListDeploymentIds("app1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid3"},
                _deploymentConfig.ListDeploymentIds("app3"));
        }

        [TestMethod]
        public void TestListDeploymentsForAppThatIsNotThere()
        {
            Assert.IsFalse(_deploymentConfig.ListDeploymentIds("UnknownApp").Any());
        }

        [TestMethod]
        public void TestListDeploymentsForVersion()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid1"},
                _deploymentConfig.ListDeploymentIds(new AppIdentity("app1", "1.0.0")));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid1", "deploymentid2"},
                _deploymentConfig.ListDeploymentIds(new AppIdentity("app1", "1.0.1")));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid3"},
                _deploymentConfig.ListDeploymentIds(new AppIdentity("app3", "2.0.0")));
        }

        [TestMethod]
        public void TestListDeploymentsForVersionThatIsNotThere()
        {
            Assert.IsFalse(_deploymentConfig.ListDeploymentIds(new AppIdentity("app1", "5.0.0")).Any());
        }

        [TestMethod]
        public void TestListDeploymentsForVersionButAppIsNotThere()
        {
            Assert.IsFalse(_deploymentConfig.ListDeploymentIds(new AppIdentity("app13", "1.0.0")).Any());
        }

        [TestMethod]
        public void TestAddDeploymentForNewApp()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app13", "1.0.13"), "deploymentid13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"app1", "app2", "app3", "app13"},
                _deploymentConfig.ListApplications());
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"1.0.13"}, _deploymentConfig.ListVersions("app13"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid13"},
                _deploymentConfig.ListDeploymentIds("app13"));
        }

        [TestMethod]
        public void TestAddDeploymentForExistingApp()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app3", "1.0.13"), "deploymentid13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"1.0.13", "2.0.0"}, _deploymentConfig.ListVersions("app3"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid3", "deploymentid13"},
                _deploymentConfig.ListDeploymentIds("app3"));
        }

        [TestMethod]
        public void TestAddDeploymentForExistingVersion()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app2", "1.0.0"), "deploymentid13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"1.0.0"}, _deploymentConfig.ListVersions("app2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid1", "deploymentid13"},
                _deploymentConfig.ListDeploymentIds("app2"));
        }

        [TestMethod, ExpectedException(typeof (InvalidOperationException))]
        public void TestAddExistingDeployment()
        {
            _deploymentConfig.AddApplication(new AppIdentity("app2", "1.0.0"), "deploymentid1");
        }

        [TestMethod]
        public void TestRemoveApplication()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication("app1");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"app2", "app3"}, _deploymentConfig.ListApplications());
        }

        [TestMethod, ExpectedException(typeof (InvalidOperationException))]
        public void TestThatRemoveApplicationForAnAppThatIsNotThere()
        {
            _deploymentConfig.RemoveApplication("UnknownApp");
        }

        [TestMethod]
        public void TestRemoveVersion()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.0"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"1.0.1"}, _deploymentConfig.ListVersions("app1"));
        }

        [TestMethod]
        public void TestThatRemoveLastVersionAlsoRemovesTheApp()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app2", "1.0.0"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"app1", "app3"}, _deploymentConfig.ListApplications());
        }

        [TestMethod, ExpectedException(typeof (InvalidOperationException))]
        public void TestRemoveVersionForAnAppThatIsNotThere()
        {
            _deploymentConfig.RemoveApplication(new AppIdentity("app13", "1.0.0"));
        }

        [TestMethod, ExpectedException(typeof (InvalidOperationException))]
        public void TestRemoveVersionThatIsNotThere()
        {
            _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.13"));
        }

        [TestMethod]
        public void TestRemoveDeployment()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.1"), "deploymentid2");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"deploymentid1"},
                _deploymentConfig.ListDeploymentIds("app1"));
        }

        [TestMethod]
        public void TestThatRemoveLastDeploymentAlsoRemovesVersion()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.0"), "deploymentid1");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"1.0.1"}, _deploymentConfig.ListVersions("app1"));
        }

        [TestMethod]
        public void TestThatRemoveLastDeploymentAlsoRemovesApplication()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app3", "2.0.0"), "deploymentid3");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] {"app1", "app2"}, _deploymentConfig.ListApplications());
        }

        [TestMethod, ExpectedException(typeof (InvalidOperationException))]
        public void TestRemoveDeploymentForAnAppThatIsNotThere()
        {
            _deploymentConfig.RemoveApplication(new AppIdentity("app13", "1.0.0"), "deploymentid13");
        }

        [TestMethod, ExpectedException(typeof (InvalidOperationException))]
        public void TestRemoveDeploymentForAVersionThatIsNotThere()
        {
            _deploymentConfig.RemoveApplication(new AppIdentity("app1", "13.0.0"), "deploymentid13");
        }

        [TestMethod, ExpectedException(typeof (InvalidOperationException))]
        public void TestRemoveADeploymentThatIsNotThere()
        {
            _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.0"), "deploymentid13");
        }

        [TestMethod]
        public void TestToJson()
        {
            string json = ReadDeploymentConfig();
            Assert.AreEqual(JObject.Parse(json).ToString(), JObject.Parse(_deploymentConfig.RawData()).ToString());
        }

        [TestMethod]
        public void TestHasApplication_appId()
        {
            Assert.IsTrue(_deploymentConfig.HasApplication("app1"));
            Assert.IsFalse(_deploymentConfig.HasApplication("app13"));
        }

        [TestMethod]
        public void TestHasApplication_appId_version()
        {
            Assert.IsTrue(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.0")));
            Assert.IsFalse(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.13")));
            Assert.IsFalse(_deploymentConfig.HasApplication(new AppIdentity("app13", "1.0.0")));
        }

        [TestMethod]
        public void TestHasApplication_appId_version_deploymentId()
        {
            Assert.IsTrue(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.0"), "deploymentid1"));
            Assert.IsTrue(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.1"), "deploymentid2"));
            Assert.IsFalse(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.0"), "deploymentid13"));
            Assert.IsFalse(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.13"), "deploymentid1"));
        }
    }
}