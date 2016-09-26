using System.Collections.Generic;
using System.Linq;
using Etg.Yams.Application;
using Etg.Yams.Json;

namespace Etg.Yams.Storage.Config
{
    public class JsonDeploymentConfigSerializer : IDeploymentConfigSerializer
    {
        private readonly IJsonSerializer _jsonSerializer;

        public JsonDeploymentConfigSerializer(IJsonSerializer jsonSerializer)
        {
            _jsonSerializer = jsonSerializer;
        }

        public DeploymentConfig Deserialize(string data)
        {
            DeploymentConfig deploymentConfig = new DeploymentConfig();
            Dictionary<string, AppDeploymentConfig> apps = new Dictionary<string, AppDeploymentConfig>();
            if (string.IsNullOrEmpty(data))
            {
                return deploymentConfig;
            }

            var appDeploymentConfigs = new List<AppDeploymentConfig>();
            ApplicationsData appsData = _jsonSerializer.Deserialize<ApplicationsData>(data);
            foreach (ApplicationData appData in appsData.Applications)
            {
                AppIdentity appIdentity = new AppIdentity(appData.Id, appData.Version);
                AppDeploymentConfig appDeploymentConfig 
                    = new AppDeploymentConfig(appIdentity, appData.TargetClusters, appData.Properties);
                appDeploymentConfigs.Add(appDeploymentConfig);
            }
            return new DeploymentConfig(appDeploymentConfigs);
        }

        public string Serialize(DeploymentConfig deploymentConfig)
        {
            var applicationsList = new List<ApplicationData>();
            foreach (AppDeploymentConfig appDeploymentConfig in deploymentConfig)
            {
                applicationsList.Add(new ApplicationData(appDeploymentConfig.AppIdentity.Id, 
                    appDeploymentConfig.AppIdentity.Version.ToString(), appDeploymentConfig.TargetClusters.ToArray(), 
                    appDeploymentConfig.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));
            }
            ApplicationsData applicationsData = new ApplicationsData(applicationsList.ToArray());
            return _jsonSerializer.Serialize(applicationsData);
        }

        private class ApplicationData
        {
            public ApplicationData(string id, string version, string[] targetClusters, Dictionary<string, string> properties)
            {
                Id = id;
                Version = version;
                TargetClusters = targetClusters;
                if (properties != null)
                {
                    Properties = properties;
                }
                else
                {
                    Properties = new Dictionary<string, string>();
                }
                
            }

            public string Id { get; private set; }
            public string Version { get; private set; }
            public string[] TargetClusters { get; private set; }
            public Dictionary<string, string> Properties { get; private set; }
        }

        private class ApplicationsData
        {
            public ApplicationsData(ApplicationData[] applications)
            {
                Applications = applications;
            }

            public ApplicationData[] Applications { get; private set; }
        }
    }
}