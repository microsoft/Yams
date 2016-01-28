using System;
using System.IO;
using System.Linq;
using Etg.Yams.Application;
using Etg.Yams.Storage.Config;
using Etg.Yams.Tools.Test.Utils;
using Xunit;
using Newtonsoft.Json.Linq;

namespace Etg.Yams.Test.Storage
{
    public class DeploymentConfigTestFixture
    {
        public DeploymentConfig DeploymentConfig { get; private set; }
        public string DeploymentConfigJson { get; private set; }

        private readonly string _deploymentConfigFilePath = Path.Combine("Data", "DeploymentConfig",
            "DeploymentConfig.json");

        public DeploymentConfigTestFixture()
        {
            DeploymentConfigJson = File.ReadAllText(_deploymentConfigFilePath);
            DeploymentConfig = new DeploymentConfig(DeploymentConfigJson);
        }
    }
    public class DeploymentConfigTest : IClassFixture<DeploymentConfigTestFixture>
    {
        private DeploymentConfig _deploymentConfig;
        private string _deplymentConfigJson;

        public DeploymentConfigTest(DeploymentConfigTestFixture fixture)
        {
            _deploymentConfig = fixture.DeploymentConfig;
            _deplymentConfigJson = fixture.DeploymentConfigJson;
        }

        [Fact]
        public void TestListApplications()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app2", "app3" },
                _deploymentConfig.ListApplications());
        }

        [Fact]
        public void TestListApplicationsForGivenDeploymentId()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app2" },
                _deploymentConfig.ListApplications("deploymentid1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1" },
                _deploymentConfig.ListApplications("deploymentid2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app3" },
                _deploymentConfig.ListApplications("deploymentid3"));
            AssertUtils.ContainsSameElementsInAnyOrder(new string[] { },
                _deploymentConfig.ListApplications("deploymentid4"));
        }

        [Fact]
        public void TestListVersions()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.0", "1.0.1" }, _deploymentConfig.ListVersions("app1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "2.0.0" }, _deploymentConfig.ListVersions("app3"));
        }

        [Fact]
        public void TestListVersionsForAnAppThatIsNotThere()
        {
            Assert.False(_deploymentConfig.ListVersions("UnknownApp").Any());
        }

        [Fact]
        public void TestListVersionsWithDeploymentId()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.0", "1.0.1" },
                _deploymentConfig.ListVersions("app1", "deploymentid1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.1" },
                _deploymentConfig.ListVersions("app1", "deploymentid2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new string[] { },
                _deploymentConfig.ListVersions("app1", "deploymentid13"));
        }

        [Fact]
        public void TestListVersionsWithDeploymentIdForAnAppThatIsNotThere()
        {
            Assert.False(_deploymentConfig.ListVersions("UnknownApp", "deploymentid1").Any());
        }

        [Fact]
        public void TestListDeploymentsForApp()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid1", "deploymentid2" },
                _deploymentConfig.ListDeploymentIds("app1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid3" },
                _deploymentConfig.ListDeploymentIds("app3"));
        }

        [Fact]
        public void TestListDeploymentsForAppThatIsNotThere()
        {
            Assert.False(_deploymentConfig.ListDeploymentIds("UnknownApp").Any());
        }

        [Fact]
        public void TestListDeploymentsForVersion()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid1" },
                _deploymentConfig.ListDeploymentIds(new AppIdentity("app1", "1.0.0")));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid1", "deploymentid2" },
                _deploymentConfig.ListDeploymentIds(new AppIdentity("app1", "1.0.1")));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid3" },
                _deploymentConfig.ListDeploymentIds(new AppIdentity("app3", "2.0.0")));
        }

        [Fact]
        public void TestListDeploymentsForVersionThatIsNotThere()
        {
            Assert.False(_deploymentConfig.ListDeploymentIds(new AppIdentity("app1", "5.0.0")).Any());
        }

        [Fact]
        public void TestListDeploymentsForVersionButAppIsNotThere()
        {
            Assert.False(_deploymentConfig.ListDeploymentIds(new AppIdentity("app13", "1.0.0")).Any());
        }

        [Fact]
        public void TestAddDeploymentForNewApp()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app13", "1.0.13"), "deploymentid13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app2", "app3", "app13" },
                _deploymentConfig.ListApplications());
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.13" }, _deploymentConfig.ListVersions("app13"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid13" },
                _deploymentConfig.ListDeploymentIds("app13"));
        }

        [Fact]
        public void TestAddDeploymentForExistingApp()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app3", "1.0.13"), "deploymentid13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.13", "2.0.0" }, _deploymentConfig.ListVersions("app3"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid3", "deploymentid13" },
                _deploymentConfig.ListDeploymentIds("app3"));
        }

        [Fact]
        public void TestAddDeploymentForExistingVersion()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app2", "1.0.0"), "deploymentid13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.0" }, _deploymentConfig.ListVersions("app2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid1", "deploymentid13" },
                _deploymentConfig.ListDeploymentIds("app2"));
        }

        [Fact]
        public void TestAddExistingDeployment()
        {
            Assert.Throws<InvalidOperationException>(() =>
            _deploymentConfig.AddApplication(new AppIdentity("app2", "1.0.0"), "deploymentid1"));
        }

        [Fact]
        public void TestRemoveApplication()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication("app1");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app2", "app3" }, _deploymentConfig.ListApplications());
        }

        [Fact]
        public void TestThatRemoveApplicationForAnAppThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
            _deploymentConfig.RemoveApplication("UnknownApp"));
        }

        [Fact]
        public void TestRemoveVersion()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.0"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.1" }, _deploymentConfig.ListVersions("app1"));
        }

        [Fact]
        public void TestThatRemoveLastVersionAlsoRemovesTheApp()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app2", "1.0.0"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app3" }, _deploymentConfig.ListApplications());
        }

        [Fact]
        public void TestRemoveVersionForAnAppThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
           _deploymentConfig.RemoveApplication(new AppIdentity("app13", "1.0.0")));
        }

        [Fact]
        public void TestRemoveVersionThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
           _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.13")));
        }

        [Fact]
        public void TestRemoveDeployment()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.1"), "deploymentid2");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "deploymentid1" },
                _deploymentConfig.ListDeploymentIds("app1"));
        }

        [Fact]
        public void TestThatRemoveLastDeploymentAlsoRemovesVersion()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.0"), "deploymentid1");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.1" }, _deploymentConfig.ListVersions("app1"));
        }

        [Fact]
        public void TestThatRemoveLastDeploymentAlsoRemovesApplication()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app3", "2.0.0"), "deploymentid3");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app2" }, _deploymentConfig.ListApplications());
        }

        [Fact]
        public void TestRemoveDeploymentForAnAppThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
           _deploymentConfig.RemoveApplication(new AppIdentity("app13", "1.0.0"), "deploymentid13"));
        }

        [Fact]
        public void TestRemoveDeploymentForAVersionThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
           _deploymentConfig.RemoveApplication(new AppIdentity("app1", "13.0.0"), "deploymentid13"));
        }

        [Fact]
        public void TestRemoveADeploymentThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
           _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.0"), "deploymentid13"));
        }

        [Fact]
        public void TestToJson()
        {
            Assert.Equal(JObject.Parse(_deplymentConfigJson).ToString(), JObject.Parse(_deploymentConfig.RawData()).ToString());
        }

        [Fact]
        public void TestHasApplication_appId()
        {
            Assert.True(_deploymentConfig.HasApplication("app1"));
            Assert.False(_deploymentConfig.HasApplication("app13"));
        }

        [Fact]
        public void TestHasApplication_appId_version()
        {
            Assert.True(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.0")));
            Assert.False(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.13")));
            Assert.False(_deploymentConfig.HasApplication(new AppIdentity("app13", "1.0.0")));
        }

        [Fact]
        public void TestHasApplication_appId_version_deploymentId()
        {
            Assert.True(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.0"), "deploymentid1"));
            Assert.True(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.1"), "deploymentid2"));
            Assert.False(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.0"), "deploymentid13"));
            Assert.False(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.13"), "deploymentid1"));
        }
    }
}