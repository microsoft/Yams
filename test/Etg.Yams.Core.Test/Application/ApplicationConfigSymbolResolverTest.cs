using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Install;
using Xunit;

namespace Etg.Yams.Test.Application
{
    public class ApplicationConfigSymbolResolverTest
    {
        [Fact]
        public async Task TestResolve()
        {
            var resolver = new ApplicationConfigSymbolResolver("clusterId", "instanceId", new Dictionary<string, string>() { {"clusterPropKey", "clusterPropValue"}});
            var appInstallConfig = new AppInstallConfig(new AppIdentity("app", "1.0.0-test"), new Dictionary<string, string> { {"appPropKey", "appPropValue"} });

            Assert.Equal("clusterId", await resolver.ResolveSymbol(appInstallConfig, "ClusterId"));
            Assert.Equal("instanceId", await resolver.ResolveSymbol(appInstallConfig, "InstanceId"));
            Assert.Equal("app", await resolver.ResolveSymbol(appInstallConfig, "Id"));
            Assert.Equal("1.0.0-test", await resolver.ResolveSymbol(appInstallConfig, "Version"));
            Assert.Equal("1", await resolver.ResolveSymbol(appInstallConfig, "Version.Major"));
            Assert.Equal("0", await resolver.ResolveSymbol(appInstallConfig, "Version.Minor"));
            Assert.Equal("0", await resolver.ResolveSymbol(appInstallConfig, "Version.Build"));
            Assert.Equal("test", await resolver.ResolveSymbol(appInstallConfig, "Version.Prerelease"));
            Assert.Equal("clusterPropValue", await resolver.ResolveSymbol(appInstallConfig, "clusterPropKey"));
            Assert.Equal("appPropValue", await resolver.ResolveSymbol(appInstallConfig, "appPropKey"));
        }

        [Fact]
        public async Task TestThatAppPropertiesOverwritesClusterProperties()
        {
            var resolver = new ApplicationConfigSymbolResolver("clusterId", "instanceId", new Dictionary<string, string>() { { "propKey", "clusterPropValue" } });
            var appInstallConfig = new AppInstallConfig(new AppIdentity("app", "1.0.0-test"), new Dictionary<string, string> { { "propKey", "appPropValue" } });

            Assert.Equal("appPropValue", await resolver.ResolveSymbol(appInstallConfig, "propKey"));
        }
    }
}