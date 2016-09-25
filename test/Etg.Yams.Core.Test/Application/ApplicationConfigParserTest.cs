using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Json;
using Etg.Yams.Storage.Config;
using Etg.Yams.Utils;
using Newtonsoft.Json.Serialization;
using Semver;
using Xunit;

namespace Etg.Yams.Test.Application
{
    public class ApplicationConfigParserTest
    {
        [Fact]
        public async Task TestParseApplicationConfig()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data\\ApplicationConfigParser\\AppConfig.json");
            const string clusterId = "clusterid1";
            const string instanceId = "instanceid1";
            
            AppIdentity identity = new AppIdentity("HelloApp", new SemVersion(1,0,0));
            Dictionary<string, string> properties = new Dictionary<string, string> {["NodeType"] = "PROD"};
            AppDeploymentConfig appDeploymentConfig = new AppDeploymentConfig(identity, new [] {clusterId}, properties);
            ApplicationConfig appConfig = await new ApplicationConfigParser(new ApplicationConfigSymbolResolver(clusterId, instanceId),
                new JsonSerializer(new DiagnosticsTraceWriter())).ParseFile(path, appDeploymentConfig);

            Assert.Equal(identity, appConfig.Identity);
            Assert.Equal("HelloApp.exe", appConfig.ExeName);
            Assert.Equal("HelloApp_1.0_instanceid1 foo bar=HelloApp_1.0_clusterid1 nodeType=PROD", appConfig.ExeArgs);
        }
    }
}
