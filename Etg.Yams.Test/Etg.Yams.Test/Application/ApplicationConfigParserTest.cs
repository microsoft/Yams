using System;
using System.IO;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Etg.Yams.Test.Application
{
    [TestClass]
    public class ApplicationConfigParserTest
    {
        [TestMethod]
        public async Task TestParseApplicationConfig()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Data\\ApplicationConfigParser\\AppConfig.json");
            const string deploymentId = "deployment_id";
            const string instanceId = "instance_id";
            AppIdentity identity = new AppIdentity("HelloApp", new Version(1,0,0));
            ApplicationConfig appConfig = await new ApplicationConfigParser(new ApplicationConfigSymbolResolver(deploymentId, instanceId)).ParseFile(path, identity);

            Assert.AreEqual(identity, appConfig.Identity);
            Assert.AreEqual("HelloApp.exe", appConfig.ExeName);
            Assert.AreEqual("HelloApp_1.0_instance_id OrleansConfiguration.xml deploymentId=HelloApp_1.0_deployment_id", appConfig.ExeArgs);
        }
    }
}
