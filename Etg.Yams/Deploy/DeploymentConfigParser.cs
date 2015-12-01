using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Etg.Yams.Application;
using Etg.Yams.Utils;

namespace Etg.Yams.Deploy
{
    public class DeploymentConfigParser
    {
        private class ApplicationData
        {
#pragma warning disable 649
            public string Id;
            public string Version;
            public string[] DeploymentIds;
#pragma warning restore 649
        }

        private class ApplicationsData
        {
#pragma warning disable 649
            public ApplicationData[] Applications;
#pragma warning restore 649
        }

        public async Task<DeploymentDirectoryConfig> ParseData(string json)
        {
            ApplicationsData data = await JsonUtils.ParseData<ApplicationsData>(json);

            var deploymentsConfigs = new List<DeploymentConfig>();
            foreach (ApplicationData appData in data.Applications)
            {
                deploymentsConfigs.Add(new DeploymentConfig(new AppIdentity(appData.Id, new Version(appData.Version)), appData.DeploymentIds));
            }

            return new DeploymentDirectoryConfig(deploymentsConfigs);
        }
    }
}