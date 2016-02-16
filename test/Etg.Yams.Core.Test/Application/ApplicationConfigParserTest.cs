using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Xunit;

namespace Etg.Yams.Test.Application
{
    public class ApplicationConfigParserTest
    {
        [Fact]
        public async Task TestParseApplicationConfig()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data\\ApplicationConfigParser\\AppConfig.json");
            const string deploymentId = "deployment_id";
            const string instanceId = "instance_id";
            AppIdentity identity = new AppIdentity("HelloApp", new Version(1,0,0));
            ApplicationConfig appConfig = await new ApplicationConfigParser(new ApplicationConfigSymbolResolver(deploymentId, instanceId)).ParseFile(path, identity);

            Assert.Equal(identity, appConfig.Identity);
            Assert.Equal("HelloApp.exe", appConfig.ExeName);
            Assert.Equal("HelloApp_1.0_instance_id OrleansConfiguration.xml deploymentId=HelloApp_1.0_deployment_id", appConfig.ExeArgs);
        }
    }
}
