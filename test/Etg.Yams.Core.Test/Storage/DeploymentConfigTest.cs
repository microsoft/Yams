using System;
using System.IO;
using System.Linq;
using Etg.Yams.Application;
using Etg.Yams.Json;
using Etg.Yams.Storage.Config;
using Etg.Yams.Test.Utils;
using Xunit;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Etg.Yams.Test.Storage
{
    public class DeploymentConfigTestFixture
    {
        public DeploymentConfig DeploymentConfig { get; private set; }
        public string DeploymentConfigJson { get; private set; }
        public IDeploymentConfigSerializer DeploymentConfigSerializer { get; }

        private readonly string _deploymentConfigFilePath = Path.Combine("Data", "DeploymentConfig",
            "DeploymentConfig.json");

        public DeploymentConfigTestFixture()
        {
            DeploymentConfigSerializer = new JsonDeploymentConfigSerializer(new JsonSerializer(new DiagnosticsTraceWriter()));
            DeploymentConfig = ParseTestDeploymentConfig();
        }

        public DeploymentConfig ParseTestDeploymentConfig()
        {
            DeploymentConfigJson = File.ReadAllText(_deploymentConfigFilePath);
            return DeploymentConfigSerializer.Deserialize(DeploymentConfigJson);
        }
    }
    public class DeploymentConfigTest : IClassFixture<DeploymentConfigTestFixture>
    {
        private readonly DeploymentConfigTestFixture _fixture;
        private DeploymentConfig _deploymentConfig;
        private readonly IDeploymentConfigSerializer _serializer;
        public DeploymentConfigTest(DeploymentConfigTestFixture fixture)
        {
            _fixture = fixture;
            _deploymentConfig = fixture.DeploymentConfig;
            _serializer = fixture.DeploymentConfigSerializer;
        }

        [Fact]
        public void TestListApplications()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app2", "app3" },
                _deploymentConfig.ListApplications());
        }

        [Fact]
        public void TestListApplicationsForGivenClusterId()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app2" },
                _deploymentConfig.ListApplications("clusterId1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1" },
                _deploymentConfig.ListApplications("clusterId2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app3" },
                _deploymentConfig.ListApplications("clusterId3"));
            AssertUtils.ContainsSameElementsInAnyOrder(new string[] { },
                _deploymentConfig.ListApplications("clusterId4"));
        }

        [Fact]
        public void TestListVersions()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.0", "1.0.1" }, _deploymentConfig.ListVersions("app1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "2.0.0-beta" }, _deploymentConfig.ListVersions("app3"));
        }

        [Fact]
        public void TestListVersionsForAnAppThatIsNotThere()
        {
            Assert.False(_deploymentConfig.ListVersions("UnknownApp").Any());
        }

        [Fact]
        public void TestListVersionsWithClusterId()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.0", "1.0.1" },
                _deploymentConfig.ListVersions("app1", "clusterId1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.1" },
                _deploymentConfig.ListVersions("app1", "clusterId2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new string[] { },
                _deploymentConfig.ListVersions("app1", "clusterId13"));
        }

        [Fact]
        public void TestListVersionsWithClusterIdForAnAppThatIsNotThere()
        {
            Assert.False(_deploymentConfig.ListVersions("UnknownApp", "clusterId1").Any());
        }

        [Fact]
        public void TestListDeploymentsForApp()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId1", "clusterId2" },
                _deploymentConfig.ListClusters("app1"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId3" },
                _deploymentConfig.ListClusters("app3"));
        }

        [Fact]
        public void TestListDeploymentsForAppThatIsNotThere()
        {
            Assert.False(_deploymentConfig.ListClusters("UnknownApp").Any());
        }

        [Fact]
        public void TestListDeploymentsForVersion()
        {
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId1" },
                _deploymentConfig.ListClusters(new AppIdentity("app1", "1.0.0")));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId1", "clusterId2" },
                _deploymentConfig.ListClusters(new AppIdentity("app1", "1.0.1")));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId3" },
                _deploymentConfig.ListClusters(new AppIdentity("app3", "2.0.0-beta")));
        }

        [Fact]
        public void TestListDeploymentsForVersionThatIsNotThere()
        {
            Assert.False(_deploymentConfig.ListClusters(new AppIdentity("app1", "5.0.0")).Any());
        }

        [Fact]
        public void TestListDeploymentsForVersionButAppIsNotThere()
        {
            Assert.False(_deploymentConfig.ListClusters(new AppIdentity("app13", "1.0.0")).Any());
        }

        [Fact]
        public void TestAddDeploymentForNewApp()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app13", "1.0.13"), "clusterId13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app2", "app3", "app13" },
                _deploymentConfig.ListApplications());
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.13" }, _deploymentConfig.ListVersions("app13"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId13" },
                _deploymentConfig.ListClusters("app13"));
        }

        [Fact]
        public void TestAddDeploymentForExistingApp()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app3", "1.0.13"), "clusterId13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.13", "2.0.0-beta" }, _deploymentConfig.ListVersions("app3"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId3", "clusterId13" },
                _deploymentConfig.ListClusters("app3"));
        }

        [Fact]
        public void TestAddDeploymentForExistingVersion()
        {
            _deploymentConfig = _deploymentConfig.AddApplication(new AppIdentity("app2", "1.0.0"), "clusterId13");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.0" }, _deploymentConfig.ListVersions("app2"));
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId1", "clusterId13" },
                _deploymentConfig.ListClusters("app2"));
        }

        [Fact]
        public void TestAddExistingDeployment()
        {
            Assert.Throws<InvalidOperationException>(() =>
            _deploymentConfig.AddApplication(new AppIdentity("app2", "1.0.0"), "clusterId1"));
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
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.1"), "clusterId2");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "clusterId1" },
                _deploymentConfig.ListClusters("app1"));
        }

        [Fact]
        public void TestThatRemoveLastDeploymentAlsoRemovesVersion()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.0"), "clusterId1");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "1.0.1" }, _deploymentConfig.ListVersions("app1"));
        }

        [Fact]
        public void TestThatRemoveLastDeploymentAlsoRemovesApplication()
        {
            _deploymentConfig = _deploymentConfig.RemoveApplication(new AppIdentity("app3", "2.0.0-beta"), "clusterId3");
            AssertUtils.ContainsSameElementsInAnyOrder(new[] { "app1", "app2" }, _deploymentConfig.ListApplications());
        }

        [Fact]
        public void TestRemoveDeploymentForAnAppThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
           _deploymentConfig.RemoveApplication(new AppIdentity("app13", "1.0.0"), "clusterId13"));
        }

        [Fact]
        public void TestRemoveDeploymentForAVersionThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
           _deploymentConfig.RemoveApplication(new AppIdentity("app1", "13.0.0"), "clusterId13"));
        }

        [Fact]
        public void TestRemoveADeploymentThatIsNotThere()
        {
            Assert.Throws<InvalidOperationException>(() =>
           _deploymentConfig.RemoveApplication(new AppIdentity("app1", "1.0.0"), "clusterId13"));
        }

        [Fact]
        public void TestSerializeRoundTrip()
        {
            string serialized = JObject.Parse(_serializer.Serialize(_deploymentConfig)).ToString();
            DeploymentConfig deploymentConfig = _serializer.Deserialize(serialized);
            string roundTripJson = JObject.Parse(_serializer.Serialize(deploymentConfig)).ToString();
            Assert.Equal(_deploymentConfig, deploymentConfig);
            Assert.Equal(serialized, roundTripJson);
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
        public void TestHasApplication_appId_version_clusterId()
        {
            Assert.True(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.0"), "clusterId1"));
            Assert.True(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.1"), "clusterId2"));
            Assert.False(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.0"), "clusterId13"));
            Assert.False(_deploymentConfig.HasApplication(new AppIdentity("app1", "1.0.13"), "clusterId1"));
        }

        [Fact]
        public void TestThatPropertiesAreParsed()
        {
            AppDeploymentConfig config = _deploymentConfig.GetApplicationConfig(new AppIdentity("app2", "1.0.0"));
            Assert.True(config.Properties.ContainsKey("NodeType"));
            Assert.Equal("PROD", config.Properties["NodeType"]);
        }

        [Fact]
        public void TestSetApplicationConfig()
        {
            AppIdentity appIdentity = new AppIdentity("newApp", "1.0.0");
            AppDeploymentConfig config = new AppDeploymentConfig(appIdentity, new [] {"clusterId1"});
            config = config.AddProperty("Foo", "Bar");
            var deploymentConfig = _deploymentConfig.SetApplicationConfig(config);
            Assert.Equal("Bar", deploymentConfig.GetApplicationConfig(appIdentity).Properties["Foo"]);
        }

        [Fact]
        public void TestThatSetApplicationConfigOverwritesExisting()
        {
            AppIdentity appIdentity = new AppIdentity("app1", "1.0.0");
            AppDeploymentConfig config = _deploymentConfig.GetApplicationConfig(appIdentity);
            config = config.AddProperty("key1", "value1");
            var deploymentConfig = _deploymentConfig.SetApplicationConfig(config);
            Assert.Equal("value1", deploymentConfig.GetApplicationConfig(appIdentity).Properties["key1"]);

            config = config.AddProperty("key2", "value2");
            deploymentConfig = deploymentConfig.SetApplicationConfig(config);
            Assert.Equal("value1", deploymentConfig.GetApplicationConfig(appIdentity).Properties["key1"]);
            Assert.Equal("value2", deploymentConfig.GetApplicationConfig(appIdentity).Properties["key2"]);
        }

        [Fact]
        public void TestEqualsAndHashCode()
        {
            DeploymentConfig deploymentConfig = _fixture.ParseTestDeploymentConfig();
            Assert.Equal(_deploymentConfig, deploymentConfig);
            Assert.Equal(_deploymentConfig.GetHashCode(), deploymentConfig.GetHashCode());
        }
    }
}